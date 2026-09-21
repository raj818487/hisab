from pathlib import Path
import re
p = Path(r"D:\Other Project\Milk-Hisab\frontend\src\app\core\api.service.ts")
t = p.read_text(encoding="utf-8")
# remove withCredentials options
t = re.sub(r",\s*\{\s*withCredentials:\s*true\s*\}", "", t)
t = re.sub(r"\{\s*withCredentials:\s*true\s*\}", "", t)

# Replace personalAuth method body with JWT response type
old_auth = re.search(r"personalAuth\([\s\S]*?\n  \}", t)
if not old_auth:
    raise SystemExit("personalAuth not found")
new_auth = """personalAuth(name: string, password: string, register: boolean) {
    return this.http.post<{
      accessToken: string;
      refreshToken: string;
      expiresAt: string;
      name: string;
    }>(""" + "`${this.base}/account/${register ? 'register' : 'login'}`" + """, { name, password });
  }"""
t = t[: old_auth.start()] + new_auth + t[old_auth.end() :]

# Ensure personalLogout exists after personalAuth
if "personalLogout" not in t:
    logout = """

  personalLogout() {
    return this.http.post(""" + "`${this.base}/account/logout`" + """, {});
  }"""
    # insert after personalAuth block
    m = re.search(r"personalAuth\([\s\S]*?\n  \}", t)
    t = t[: m.end()] + logout + t[m.end() :]

p.write_text(t, encoding="utf-8")
print("patched OK, length", len(t))
# show auth region
i = t.find("personalSession")
print(t[i : i + 900])
