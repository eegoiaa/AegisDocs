using AegisDocs.UI.Models;
using System.Collections.Generic;

namespace AegisDocs.UI.Services;

public static class PromptProvider
{
    private const string JsonFormatRequirement = @"
        ТВОЯ ЗАДАЧА — ВЕРНУТЬ ОТВЕТ ИСКЛЮЧИТЕЛЬНО В ФОРМАТЕ JSON. 
        Не пиши никаких приветствий, пояснений или текста до и после JSON. 
        Обязательно классифицируй каждую ошибку в поле Category.
        СТРОГО ЗАПРЕЩЕНО писать любой текст после закрывающей скобки ] !!!
        Формат ответа должен быть строго таким:
        [
          {
            ""Category"": ""Название категории"",
            ""OriginalText"": ""ошибочный фрагмент из текста"",
            ""CorrectedText"": ""исправленный вариант"",
            ""Reason"": ""почему это ошибка""
          }
        ]";

    public static List<PromptTemplate> GetDefaultTemplates()
    {
        return new List<PromptTemplate>
        {
            new PromptTemplate(
                "Глубокий аудит (Все ошибки)",
                "Ты строгий юрист-аудитор. Проверь текст договора на наличие любых ошибок: логических, финансовых, фактических, в датах и реквизитах. " + JsonFormatRequirement
            ),

            new PromptTemplate(
                "Проверка дат, сроков и сумм",
                "Ты математик-аудитор. Твоя ЕДИНСТВЕННАЯ задача — проверить все даты, сроки, проценты и суммы в тексте. Ищи логические и математические нестыковки. НЕ придумывай новые условия. " + JsonFormatRequirement
            ),

            new PromptTemplate(
                "Аудит реквизитов и сторон",
                "Ты юрист-регистратор. Твоя задача — проверить только правильность заполнения данных сторон (ИНН, КПП, расчетные счета, паспорта, ФИО и должности подписантов). Ищи опечатки и недостающие данные. " + JsonFormatRequirement
            )
        };
    }
}
