using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirebaseAdmin;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Domain.Entitites;
using Google.Cloud.Firestore.V1;
using Microsoft.AspNetCore.Mvc;

namespace Application.Service
{
    public class FirebaseService
    {
        private readonly FirestoreDb _db;

        public FirebaseService(FirestoreDb db)
        {
            _db = db;
        }
        public FirestoreDb GetFirestoreDb()
        {
            return _db;
        }
        public async Task GetMessages()
        {
            var snapshot = await _db.Collection("messages")
                                           .OrderBy("Timestamp")
                                           .Limit(20) // last 20 messages
                                           .GetSnapshotAsync();

            var messages = snapshot.Documents.Select(d => d.ConvertTo<ChatMessage>());
        }
        public async Task SaveChatMessage(string chatId, string userId, string message)
        {
            var docRef = _db.Collection("chats").Document(chatId)
                             .Collection("messages").Document();
            await docRef.SetAsync(new
            {
                userId,
                message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
