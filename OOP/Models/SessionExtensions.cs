using System.Text.Json;

namespace OOP.Models
{
    public static class SessionExtensions
    {
        public static void SetObject<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            if (value == null) return default(T);

            // Abstract sınıfları ve karmaşık tipleri doğru deserialize etmek için
            // Polimorfizm desteği gerekebilir (JsonSerializerOptions). 
            // Basit GameBoard yapısı için bu yöntem yeterlidir.
            return JsonSerializer.Deserialize<T>(value);
        }
    }
}