from pathlib import Path
import re
p = Path(r"D:\Other Project\Milk-Hisab\frontend\src\app\core\api.service.ts")
t = p.read_text(encoding="utf-8")
t = re.sub(r",\s*\{\s*withCredentials:\s*true\s*\}", "", t)
t = re.sub(r"\{\s*withCredentials:\s*true\s*\}", "", t)
# Fix empty options leftover like post(url, body, ) or get(url, )
t = re.sub(r",\s*\)", ")", t)

# Replace only the response type of personalAuth: post<{name:string}> -> JWT type
jwt_type = "{ accessToken: string; refreshToken: string; expiresAt: string; name: string }"
t2, n = re.subn(
    r"(personalAuth\([^\)]*\)\s*\{\s*return\s*this\.http\.post)<\{name:string\}>",
    r"\1<" + jwt_type + ">",
    t,
    count=1,
)
if n != 1:
    # try multiline / spaced variant
    t2, n = re.subn(
        r"(personalAuth\([\s\S]*?this\.http\.post)<\s*\{\s*name\s*:\s*string\s*\}\s*>",
        r"\1<" + jwt_type + ">",
        t,
        count=1,
    )
print("auth type replacements:", n)
if n != 1:
    raise SystemExit("failed to patch personalAuth type")
p.write_text(t2, encoding="utf-8")
# verify personal methods still present
for name in ["personalSession","personalAuth","personalLogout","personalData","personalProduct","personalExpense","personalPay","personalExpenseUpdate","personalExpenseDelete","personalProductDelete","getCustomers","downloadReceiptHtml"]:
    print(name, "OK" if name in t2 else "MISSING")
