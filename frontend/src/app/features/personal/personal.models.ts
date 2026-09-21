export interface Product {
  id: number;
  name: string;
  unit: string;
  rate: number;
}
export interface Expense {
  id: number;
  date: string;
  title: string;
  category: string;
  supplier?: string;
  productId?: number;
  quantity?: number;
  rate?: number;
  amount: number;
  note?: string;
}
export interface ExpensePayment {
  id: number;
  expenseId: number;
  date: string;
  amount: number;
  mode: string;
}
export interface PersonalData {
  products: Product[];
  expenses: Expense[];
  payments: ExpensePayment[];
}
export interface ExpenseDraft {
  date: string;
  title: string;
  category: string;
  supplier: string;
  productId: number | null;
  quantity: number;
  rate: number;
  amount: number;
  paid: number;
  mode: string;
  note: string;
}
