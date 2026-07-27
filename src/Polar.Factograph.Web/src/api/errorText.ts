import { ApiRequestError } from "./http";

const messages: Record<string, string> = {
  authentication_required: "Сначала войдите в систему.",
  invalid_credentials: "Неверный логин или пароль.",
  invalid_registration: "Проверьте логин и пароль.",
  registration_unavailable: "Регистрация сейчас недоступна.",
  identity_storage_unavailable: "Не удалось сохранить данные пользователя. Повторите попытку.",
  antiforgery_failed: "Сессия формы устарела. Обновите страницу и повторите попытку.",
  forbidden: "Недостаточно прав для этой операции.",
  resource_not_found: "Ресурс не найден или недоступен.",
  document_not_found: "Документ не найден или недоступен.",
  document_variant_not_found: "Запрошенный вариант документа ещё не создан.",
  project_unavailable: "Проект временно недоступен.",
  storage_unavailable: "Индекс временно недоступен.",
  invalid_request: "Запрос не прошёл проверку."
};

export function errorText(error: unknown): string {
  if (error instanceof ApiRequestError) {
    return messages[error.code] ?? error.message;
  }
  if (error instanceof Error && error.name !== "AbortError") {
    return error.message;
  }
  return "Не удалось выполнить запрос.";
}
