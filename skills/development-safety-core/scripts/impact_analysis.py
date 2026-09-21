#!/usr/bin/env python3
"""Graphify impact closure processed in mandatory, unlimited sequential batches."""
from __future__ import annotations
import argparse
from collections import deque
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import re

DEFAULTS = {
    "requireGraphLinks": True, "requireImpactManifest": True, "batchSize": 25,
    "relations": ["*"],
}
CLASSIFICATIONS = {"must_change", "must_inspect", "verify_only", "ignore", "unknown"}

def load_config(root):
    path = root / "dev-assistant.json"
    return json.loads(path.read_text(encoding="utf-8-sig")) if path.exists() else {}

def settings(root):
    result = dict(DEFAULTS)
    configured = load_config(root).get("impactAnalysis", {})
    if isinstance(configured, dict): result.update(configured)
    result["batchSize"] = max(1, int(result.get("batchSize", 25)))
    result["relations"] = {str(value) for value in result.get("relations", [])}
    return result

def graph_path(root):
    configured = load_config(root).get("graphPath")
    return Path(configured).resolve() if configured else root / "graphify-out" / "graph.json"

def links(data):
    value = data.get("links")
    return value if isinstance(value, list) else data.get("edges", [])

def manifest_path(root): return root / ".ai-work" / "impact-manifest.json"
def normalize(value): return re.sub(r"[^a-z0-9]", "", value.lower())

def start(root, seeds):
    path = graph_path(root)
    if not path.exists(): raise RuntimeError(f"Graphify graph not found: {path}")
    data = json.loads(path.read_text(encoding="utf-8"))
    graph_links, config = links(data), settings(root)
    if config["requireGraphLinks"] and not graph_links:
        raise RuntimeError("Graphify graph has no dependency links; impact is unverified")
    nodes = {str(node["id"]): node for node in data.get("nodes", []) if "id" in node}
    wanted = {normalize(seed) for seed in seeds if seed.strip()}
    matched = {node_id for node_id, node in nodes.items() if wanted.intersection({
        normalize(node_id), normalize(str(node.get("label", ""))),
        normalize(str(node.get("norm_label", "")))})}
    matched_values = {normalize(str(value)) for node_id in matched for value in
                      (node_id, nodes[node_id].get("label", ""), nodes[node_id].get("norm_label", ""))}
    missing_seeds = sorted(seed for seed in seeds if normalize(seed) not in matched_values)
    if not matched: raise RuntimeError(f"No exact Graphify nodes matched: {', '.join(seeds)}")
    adjacency = {}
    for edge in graph_links:
        if "*" not in config["relations"] and str(edge.get("relation")) not in config["relations"]: continue
        source, target = str(edge.get("source")), str(edge.get("target"))
        adjacency.setdefault(source, []).append((target, edge))
        adjacency.setdefault(target, []).append((source, edge))
    discovered, queue = set(matched), deque(sorted(matched))
    evidence = {node_id: [] for node_id in matched}
    while queue:
        current = queue.popleft()
        for neighbour, edge in adjacency.get(current, []):
            evidence.setdefault(neighbour, []).append({
                "from": current, "relation": edge.get("relation"),
                "sourceFile": edge.get("source_file", ""),
                "sourceLocation": edge.get("source_location", "")})
            if neighbour not in discovered:
                discovered.add(neighbour); queue.append(neighbour)
    items = []
    for node_id in sorted(discovered):
        node = nodes.get(node_id, {})
        files = sorted({str(value) for value in (
            node.get("source_file", ""),
            *(entry.get("sourceFile", "") for entry in evidence.get(node_id, []))) if value})
        items.append({"id": node_id, "label": node.get("label", node_id), "files": files,
                      "classification": None, "reason": "", "evidence": evidence.get(node_id, [])})
    size = config["batchSize"]
    manifest = {"version": 1, "createdAt": datetime.now(timezone.utc).isoformat(),
                "graph": str(path), "graphSha256": hashlib.sha256(path.read_bytes()).hexdigest(), "seeds": seeds, "missingSeeds": missing_seeds,
                "batchSize": size, "totalBatches": (len(items)+size-1)//size, "items": items}
    output = manifest_path(root); output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    return status(root, True)

def load_manifest(root):
    path = manifest_path(root)
    if not path.exists(): raise RuntimeError(f"Impact manifest not found: {path}; run start first")
    return json.loads(path.read_text(encoding="utf-8"))

def status(root, include_batch=False):
    manifest = load_manifest(root)
    pending = [item for item in manifest["items"] if not item.get("classification")]
    size, completed = int(manifest["batchSize"]), len(manifest["items"])-len(pending)
    result = {"status": "pass" if not pending and not manifest.get("missingSeeds") else "in_progress",
              "total": len(manifest["items"]), "classified": completed, "pending": len(pending),
              "batchSize": size, "completedBatches": manifest["totalBatches"] if not pending else completed//size,
              "totalBatches": manifest["totalBatches"], "missingSeeds": manifest.get("missingSeeds", []),
              "continuationRequired": bool(pending)}
    if include_batch: result["nextBatch"] = pending[:size]
    return result

def classify(root, ids, classification, reason):
    if classification not in CLASSIFICATIONS: raise RuntimeError(f"Invalid classification: {classification}")
    if not reason.strip(): raise RuntimeError("A classification reason is required")
    manifest, wanted, found = load_manifest(root), set(ids), set()
    for item in manifest["items"]:
        if item["id"] in wanted:
            item["classification"], item["reason"] = classification, reason.strip(); found.add(item["id"])
    missing = sorted(wanted-found)
    if missing: raise RuntimeError(f"Impact IDs not found: {', '.join(missing)}")
    manifest_path(root).write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    return status(root, True)

def verify_complete(root, targets=None):
    if not settings(root)["requireImpactManifest"]: return {"status": "disabled"}
    manifest = load_manifest(root)
    current_graph = graph_path(root)
    current_digest = hashlib.sha256(current_graph.read_bytes()).hexdigest() if current_graph.exists() else ""
    if manifest.get("graphSha256") != current_digest:
        raise RuntimeError("Impact manifest is stale after graph changes; run start again and process every batch")
    pending = [item["id"] for item in manifest["items"] if not item.get("classification")]
    if pending: raise RuntimeError(f"Impact analysis incomplete: {len(pending)} items remain. Batch size is not a stopping limit; process the next batch.")
    if manifest.get("missingSeeds"): raise RuntimeError(f"Impact seeds unverified: {', '.join(manifest['missingSeeds'])}")
    unknown = [item["id"] for item in manifest["items"] if item.get("classification") == "unknown"]
    if unknown: raise RuntimeError(f"Unknown impact items require resolution: {', '.join(unknown)}")
    must_change = {file for item in manifest["items"] if item.get("classification") == "must_change"
                   for file in item.get("files", [])}
    if targets is not None:
        missing = sorted(must_change-set(targets))
        if missing: raise RuntimeError(f"Must-change files missing from baseline: {', '.join(missing)}")
    return {"status": "pass", "mustChangeFiles": sorted(must_change), "items": len(manifest["items"])}

def parser():
    result = argparse.ArgumentParser(description="Mandatory Graphify impact closure")
    sub = result.add_subparsers(dest="command", required=True)
    command = sub.add_parser("start"); command.add_argument("--seeds", nargs="+", required=True)
    sub.add_parser("next"); sub.add_parser("status")
    command = sub.add_parser("classify")
    command.add_argument("--ids", nargs="+", required=True)
    command.add_argument("--as", dest="classification", choices=sorted(CLASSIFICATIONS), required=True)
    command.add_argument("--reason", required=True)
    command = sub.add_parser("verify"); command.add_argument("--targets", nargs="*")
    return result

def main():
    args, root = parser().parse_args(), Path.cwd().resolve()
    try:
        if args.command == "start": result = start(root, args.seeds)
        elif args.command in ("next", "status"): result = status(root, args.command == "next")
        elif args.command == "classify": result = classify(root, args.ids, args.classification, args.reason)
        else: result = verify_complete(root, args.targets)
    except Exception as exc: result = {"status": "fail", "error": str(exc)}
    print(json.dumps(result, indent=2))
    return 0 if result.get("status") in ("pass", "in_progress") else 1

if __name__ == "__main__": raise SystemExit(main())
