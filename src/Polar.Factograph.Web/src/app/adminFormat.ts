export function formatAdminDate(value: string | null): string {
  if (value === null) return "—";
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? value : date.toLocaleString("ru-RU");
}

export function formatAdminNumber(value: number): string {
  return value.toLocaleString("ru-RU");
}

export function indexStateLabel(value: string): string {
  return ({
    ready: "готов",
    dirty: "требует перестроения",
    missing: "отсутствует",
    invalid: "повреждён"
  } as Record<string, string>)[value] ?? value;
}

export function workerStateLabel(value: string): string {
  return ({
    disabled: "отключён",
    starting: "запускается",
    working: "работает",
    idle: "ожидает",
    degraded: "есть ошибки",
    unresponsive: "не отвечает",
    stopped: "остановлен"
  } as Record<string, string>)[value] ?? value;
}
