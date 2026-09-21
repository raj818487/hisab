from pathlib import Path
p = Path(r"D:\Other Project\Milk-Hisab\frontend\src\app\core\api.service.ts")
text = p.read_text(encoding="utf-8")
start = text.find("  personalSession()")
end = text.find("  // --- Customers ---")
if start < 0 or end < 0:
    raise SystemExit(f"markers not found start={start} end={end}")

# Use normal Python format with explicit braces for TS template strings
b = "this.base"
block = f"""  personalSession() {{
    return this.http.get<{{ name: string }}>(`${{{b}}}/account`);
  }}

  personalAuth(name: string, password: string, register: boolean) {{
    return this.http.post<{{
      accessToken: string;
      refreshToken: string;
      expiresAt: string;
      name: string;
    }}>(`${{{b}}}/account/${{register ? 'register' : 'login'}}`, {{ name, password }});
  }}

  personalLogout() {{
    return this.http.post(`${{{b}}}/account/logout`, {{}});
  }}

  personalData() {{
    return this.http.get<PersonalData>(`${{{b}}}/personal`);
  }}

  personalProduct(name: string, unit: string, rate: number) {{
    return this.http.post<Product>(`${{{b}}}/personal/products`, {{ name, unit, rate }});
  }}

  personalExpense(body: ExpenseDraft) {{
    return this.http.post<Expense>(`${{{b}}}/personal/expenses`, body);
  }}

  personalPay(id: number, date: string, amount: number, mode: string) {{
    return this.http.post<ExpensePayment>(
      `${{{b}}}/personal/expenses/${{id}}/payments`,
      {{ date, amount, mode }},
    );
  }}

  personalExpenseUpdate(id: number, body: ExpenseDraft) {{
    return this.http.put<Expense>(`${{{b}}}/personal/expenses/${{id}}`, body);
  }}

  personalExpenseDelete(id: number) {{
    return this.http.delete(`${{{b}}}/personal/expenses/${{id}}`);
  }}

  personalProductDelete(id: number) {{
    return this.http.delete(`${{{b}}}/personal/products/${{id}}`);
  }}

"""
rest = text[end:]
# dedupe downloadReceiptHtml
import re
ms = list(re.finditer(r"\n  downloadReceiptHtml\([\s\S]*?\n  \}", rest))
if len(ms) > 1:
    for m in reversed(ms[1:]):
        rest = rest[: m.start()] + rest[m.end() :]
out = text[:start] + block + rest
p.write_text(out, encoding="utf-8")
print("OK")
print(out[start : start + 600])
