using API_Server.DTOs;
using API_Server.Models;
using MongoDB.Driver;
using UglyToad.PdfPig;
using System.Text.Json;
using System.Net.Http;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;


namespace API_Server.Services
{
    public class QuestionService
    {
        private readonly IMongoCollection<ListQuestion> _questions;

        public QuestionService(MongoDbService context, HttpClient httpClient)
        {
            _questions = context.ListQuestions;
        }

        public async Task<QuestionDTO> CreateQuestionAsync(Question _question, string owner)
        {
            var question = new Question
            {
                Topic = _question.Topic,
                Content = _question.Content,
                CorrectAnswer = _question.CorrectAnswer,
                Owner = owner,
            };

            var listQuestion = new ListQuestion
            {
                Owner = question.Owner,
                Questions = new List<Question> { question }
            };

            var filter = Builders<ListQuestion>.Filter.Eq(q => q.Owner, owner);
            var listQuestionExists = await _questions.Find(filter).FirstOrDefaultAsync();

            if (listQuestionExists != null)
            {
                var update = Builders<ListQuestion>.Update.PushEach(q => q.Questions, listQuestion.Questions);
                await _questions.UpdateOneAsync(filter, update);
            }
            else
            {
                await _questions.InsertOneAsync(listQuestion);
            }

            var questionDto = new QuestionDTO
            {
                Topic = question.Topic,
                Content = question.Content,
                CorrectAnswer = question.CorrectAnswer
            };

            return questionDto;
        }

        public async Task<bool> IsContentExistsAsync(string content, string owner)
        {
            var filter = Builders<ListQuestion>.Filter.And(
                        Builders<ListQuestion>.Filter.Eq(q => q.Owner, owner),
                        Builders<ListQuestion>.Filter.ElemMatch(q => q.Questions, q => q.Content == content)
            );

            var result = await _questions.Find(filter).FirstOrDefaultAsync();
            if (result != null)
            {
                return true;
            }
            return false;
        }

        public async Task<Question> GetQuestionAsync(string content, string owner)
        {
            var filter = Builders<ListQuestion>.Filter.And(
                Builders<ListQuestion>.Filter.Eq(q => q.Owner, owner),
                Builders<ListQuestion>.Filter.ElemMatch(q => q.Questions, q => q.Content == content)
            );

            var projection = Builders<ListQuestion>.Projection.Expression(list => list.Questions
                .Where(q => q.Content == content)
                .FirstOrDefault());

            var question = await _questions.Find(filter).Project(projection).FirstOrDefaultAsync();

            return question;
        }
        public async Task<QuestionDTO> GetRandomQuestionAsync(string owner)
        {
            var filter = Builders<ListQuestion>.Filter.Eq(q => q.Owner, owner);
            var listQuestion = await _questions.Find(filter).FirstOrDefaultAsync();

            if (listQuestion != null && listQuestion.Questions.Any())
            {
                var random = new Random();
                var randomQuestion = listQuestion.Questions[random.Next(listQuestion.Questions.Count)];

                var questionDto = new QuestionDTO
                {
                    Topic = randomQuestion.Topic,
                    Content = randomQuestion.Content,
                    CorrectAnswer = randomQuestion.CorrectAnswer
                };

                return questionDto;
            }

            return null;

        }

        public async Task<List<QuestionDTO>> GetAllQuestionsAsync(string owner)
        {
            var filter = Builders<ListQuestion>.Filter.Eq(q => q.Owner, owner);

            var listQuestion = await _questions.Find(filter).FirstOrDefaultAsync();

            if (listQuestion?.Questions == null)
            {
                return new List<QuestionDTO>();
            }

            return listQuestion.Questions.Select(question => new QuestionDTO
            {
                Topic = question.Topic,
                Content = question.Content,
                CorrectAnswer = question.CorrectAnswer
            }).ToList();
        }


        public async Task<QuestionDTO> GetRandomQuestionByTopic(string owner, string topic)
        {
            var filter = Builders<ListQuestion>.Filter.And(
                Builders<ListQuestion>.Filter.Eq(q => q.Owner, owner),
                Builders<ListQuestion>.Filter.ElemMatch(q => q.Questions, q => q.Topic == topic)
            );

            var projection = Builders<ListQuestion>.Projection.Expression(list => list.Questions
                .Where(q => q.Topic == topic)
                .ToList());

            var filteredQuestions = await _questions.Find(filter).Project(projection).FirstOrDefaultAsync();

            if (filteredQuestions != null && filteredQuestions.Any())
            {
                var random = new Random();
                var randomQuestion = filteredQuestions[random.Next(filteredQuestions.Count)];

                return new QuestionDTO
                {
                    Topic = randomQuestion.Topic,
                    Content = randomQuestion.Content,
                    CorrectAnswer = randomQuestion.CorrectAnswer
                };
            }

            return null;
        }


        public async Task<List<QuestionDTO>> GetAllQuestionByTopic(string owner, string topic)
        {
            var filter = Builders<ListQuestion>.Filter.And(
                Builders<ListQuestion>.Filter.Eq(q => q.Owner, owner),
                Builders<ListQuestion>.Filter.ElemMatch(q => q.Questions, q => q.Topic == topic)
            );

            var projection = Builders<ListQuestion>.Projection.Expression(list => list.Questions
                .Where(q => q.Topic == topic)
                .ToList());

            var filteredQuestions = await _questions.Find(filter).Project(projection).FirstOrDefaultAsync();

            if (filteredQuestions == null || !filteredQuestions.Any())
            {
                return new List<QuestionDTO>();
            }

            return filteredQuestions.Select(question => new QuestionDTO
            {
                Topic = question.Topic,
                Content = question.Content,
                CorrectAnswer = question.CorrectAnswer
            }).ToList();
        }

        public async Task<bool> DeleteQuestion(QuestionDTO question, string owner)
        {
            var filter = Builders<ListQuestion>.Filter.And(
                Builders<ListQuestion>.Filter.Eq(q => q.Owner, owner),
                Builders<ListQuestion>.Filter.ElemMatch(q => q.Questions,
                         q => q.Topic == question.Topic &&
                         q.Content == question.Content &&
                         q.CorrectAnswer == question.CorrectAnswer)
            );

            var update = Builders<ListQuestion>.Update.PullFilter(q => q.Questions,
                q => q.Topic == question.Topic &&
                     q.Content == question.Content &&
                     q.CorrectAnswer == question.CorrectAnswer
            );

            var result = await _questions.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }

        public async Task<string> GetCorrectAnswer(string content, string owner)
        {
            var question = await GetQuestionAsync(content, owner);
            return question.CorrectAnswer;
        }

        public async Task<QuestionDTO> UpdateQuestion(QuestionDTO oldQuestion, QuestionDTO newQuestion, string owner)
        {
            var filter = Builders<ListQuestion>.Filter.And(
                Builders<ListQuestion>.Filter.Eq(q => q.Owner, owner),
                Builders<ListQuestion>.Filter.ElemMatch(
                    q => q.Questions,
                    q => q.Topic == oldQuestion.Topic &&
                         q.Content == oldQuestion.Content &&
                         q.CorrectAnswer == oldQuestion.CorrectAnswer
                )
            );

            var update = Builders<ListQuestion>.Update
                .Set("Questions.$.Topic", newQuestion.Topic ?? oldQuestion.Topic)
                .Set("Questions.$.Content", newQuestion.Content ?? oldQuestion.Content)
                .Set("Questions.$.CorrectAnswer", newQuestion.CorrectAnswer ?? oldQuestion.CorrectAnswer);

            var result = await _questions.UpdateOneAsync(filter, update);

            if (result.ModifiedCount > 0)
            {
                return newQuestion;
            }

            return null;
        }


    }
}
