using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace RedditService.Models
{
    /// <summary>
    /// Privatna poruka između dva korisnika, čuva se u Azure Table Storage.
    ///
    /// Dizajn ključeva:
    ///  - PartitionKey = ID konverzacije (dve e-mail adrese sortirane i spojene sa '|').
    ///    Tako sve poruke jedne konverzacije žive u istoj particiji => brz i jeftin upit.
    ///  - RowKey       = GUID (jedinstven ID poruke), koristi se za edit/delete.
    ///
    /// Brisanje je HARD delete (TableOperation.Delete) => poruka fizički nestaje
    /// iz baze i time i za pošiljaoca i za primaoca (nema soft-delete placeholdera).
    /// </summary>
    public class Message : TableEntity
    {
        public string Sender { get; set; }     // e-mail pošiljaoca
        public string Receiver { get; set; }   // e-mail primaoca
        public string Content { get; set; }
        public DateTime SentAt { get; set; }   // vreme slanja (UTC) -> zahtevani "Timestamp"
        public bool IsEdited { get; set; }

        public Message() { }

        public Message(string sender, string receiver, string content)
        {
            PartitionKey = BuildConversationId(sender, receiver);
            RowKey = Guid.NewGuid().ToString("N");
            Sender = sender;
            Receiver = receiver;
            Content = content;
            SentAt = DateTime.UtcNow;
            IsEdited = false;
        }

        /// <summary>
        /// Deterministički ID konverzacije: e-mailovi se sortiraju pa spajaju,
        /// tako da (A,B) i (B,A) daju istu particiju.
        /// </summary>
        public static string BuildConversationId(string emailA, string emailB)
        {
            string a = (emailA ?? string.Empty).Trim().ToLowerInvariant();
            string b = (emailB ?? string.Empty).Trim().ToLowerInvariant();
            return string.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a;
        }
    }
}
