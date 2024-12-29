using API_Server.Models;
using MongoDB.Driver;
using API_Server.DTOs;

namespace API_Server.Services
{
    public class TaskService
    {
        private readonly IMongoCollection<ListTask> _listTask;

        public TaskService(MongoDbService context, HttpClient httpClient)
        {
            _listTask = context.ListTasks;
        }

        public async Task<TaskDTO> CreateTaskAsync(UserTask _task, string owner)
        {
            var task = new UserTask
            {
                Description = _task.Description,
                Category = _task.Category,
                IsCompleted = _task.IsCompleted,
                StartDate = _task.StartDate.Date,
                EndDate = _task.EndDate.Date,
                Owner = owner
            };

            var listTask = new ListTask
            {
                Owner = task.Owner,
                Tasks = new List<UserTask> { task }
            };

            var filter = Builders<ListTask>.Filter.Eq(q => q.Owner, owner);
            var listTaskExists = await _listTask.Find(filter).FirstOrDefaultAsync();

            if (listTaskExists != null)
            {
                var update = Builders<ListTask>.Update.PushEach(q => q.Tasks, listTask.Tasks);
                await _listTask.UpdateOneAsync(filter, update);
            }
            else
            {
                await _listTask.InsertOneAsync(listTask);
            }

            var taskDto = new TaskDTO
            {
                Description = task.Description,
                Category = task.Category,
                IsCompleted = task.IsCompleted,
                StartDate = task.StartDate.Date,
                EndDate = task.EndDate.Date
            };

            return taskDto;
        }

        public async Task<List<TaskDTO>> GetTasksAsync(string owner)
        {
            var filter = Builders<ListTask>.Filter.Eq(q => q.Owner, owner);
            var listTask = await _listTask.Find(filter).FirstOrDefaultAsync();

            if(listTask == null)
            {
                return new List<TaskDTO>();
            }

            var taskListDto = listTask.Tasks.Select(task => new TaskDTO
            {
               Description = task.Description,
               Category = task.Category,
               IsCompleted = task.IsCompleted,
               StartDate = task.StartDate.Date,
               EndDate = task.EndDate.Date
            }).ToList();

            return taskListDto;
        }

        public async Task<List<TaskDTO>> GetTaskByCategoryAsync(string owner, string category)
        {
            var filter = Builders<ListTask>.Filter.And(
                        Builders<ListTask>.Filter.Eq(q => q.Owner, owner),
                        Builders<ListTask>.Filter.ElemMatch(q => q.Tasks, q => q.Category == category)
            );

            var projection = Builders<ListTask>.Projection.Expression(list => list.Tasks
                .Where(q => q.Category == category)
                .ToList());

            var listTask = await _listTask.Find(filter).Project(projection).FirstOrDefaultAsync();

            var taskListDto = listTask.Select(task => new TaskDTO
            {
                Description = task.Description,
                Category = task.Category,
                IsCompleted = task.IsCompleted,
                StartDate = task.StartDate.Date,
                EndDate = task.EndDate.Date
            }).ToList();

            return taskListDto;
        }

        public async Task<List<TaskDTO>> GetTaskByIsComplete(string owner)
        {
            var filter = Builders<ListTask>.Filter.And(
                        Builders<ListTask>.Filter.Eq(q => q.Owner, owner),
                        Builders<ListTask>.Filter.ElemMatch(q => q.Tasks, q => q.IsCompleted == true)
            );

            var projection = Builders<ListTask>.Projection.Expression(list => list.Tasks
                .Where(q => q.IsCompleted == true)
                .ToList());

            var listTask = await _listTask.Find(filter).Project(projection).FirstOrDefaultAsync();

            var taskListDto = listTask.Select(task => new TaskDTO
            {
                Description = task.Description,
                Category = task.Category,
                IsCompleted = task.IsCompleted,
                StartDate = task.StartDate.Date,
                EndDate = task.EndDate.Date
            }).ToList();

            return taskListDto;
        }

        public async Task<List<TaskDTO>> GetTaskIsPending(string owner)
        {
            var filter = Builders<ListTask>.Filter.And(
                        Builders<ListTask>.Filter.Eq(q => q.Owner, owner),
                        Builders<ListTask>.Filter.ElemMatch(q => q.Tasks, q => q.IsCompleted == false)
            );

            var projection = Builders<ListTask>.Projection.Expression(list => list.Tasks
                .Where(q => q.IsCompleted == false)
                .ToList());

            var listTask = await _listTask.Find(filter).Project(projection).FirstOrDefaultAsync();

            var taskListDto = listTask.Select(task => new TaskDTO
            {
                Description = task.Description,
                Category = task.Category,
                IsCompleted = task.IsCompleted,
                StartDate = task.StartDate.Date,
                EndDate = task.EndDate.Date
            }).ToList();

            return taskListDto;
        }

        public async Task<bool> DeleteTask(string owner, TaskDTO task)
        {
            var filter = Builders<ListTask>.Filter.And(
                Builders<ListTask>.Filter.Eq(q => q.Owner, owner),
                Builders<ListTask>.Filter.ElemMatch(q => q.Tasks,
                         q => q.Description == task.Description &&
                         q.Category == task.Category &&
                         q.StartDate == task.StartDate && q.EndDate == task.EndDate &&
                         q.IsCompleted == task.IsCompleted)
            );

            var update = Builders<ListTask>.Update.PullFilter(q => q.Tasks,
                q => q.Description == task.Description &&
                     q.Category == task.Category &&
                     q.StartDate == task.StartDate && q.EndDate == task.EndDate &&
                     q.IsCompleted == task.IsCompleted);

            var result = await _listTask.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }

        public async Task<List<TaskDTO>> GetTaskByDate(string owner, DateTime date)
        {
            var filter = Builders<ListTask>.Filter.And(
                Builders<ListTask>.Filter.Eq(q => q.Owner, owner),
                Builders<ListTask>.Filter.ElemMatch(q => q.Tasks,
                    q => q.StartDate >= date.Date && q.StartDate < date.Date.AddDays(1))
            );

            var projection = Builders<ListTask>.Projection.Expression(list => list.Tasks);
            var listTask = await _listTask.Find(filter).Project(projection).FirstOrDefaultAsync();

            if (listTask == null)
            {
                return new List<TaskDTO>();
            }

            var taskListDto = listTask
                .Where(task => task.StartDate.Date == date.Date)
                .Select(task => new TaskDTO
                {
                    Description = task.Description,
                    Category = task.Category,
                    IsCompleted = task.IsCompleted,
                    StartDate = task.StartDate.Date,
                    EndDate = task.EndDate.Date
                })
                .ToList();

            return taskListDto;
        }

        public async Task<bool> UpdateTask(string owner, TaskDTO oldtask, TaskDTO newtask)
        {
            var filter = Builders<ListTask>.Filter.And(
                Builders<ListTask>.Filter.Eq(q => q.Owner, owner),
                Builders<ListTask>.Filter.ElemMatch(q => q.Tasks,
                         q => q.Description == oldtask.Description &&
                         q.Category == oldtask.Category &&
                         q.StartDate == oldtask.StartDate && q.EndDate == oldtask.EndDate &&
                         q.IsCompleted == oldtask.IsCompleted)
            );

            var update = Builders<ListTask>.Update
                .Set("Tasks.$.Description", newtask.Description ?? oldtask.Description)
                .Set("Tasks.$.Category", newtask.Category ?? oldtask.Category)
                .Set("Tasks.$.IsCompleted", newtask.IsCompleted ?? oldtask.IsCompleted)
                .Set("Tasks.$.StartDate", newtask.StartDate.Date)
                .Set("Tasks.$.EndDate", newtask.EndDate.Date);

            var result = await _listTask.UpdateOneAsync(filter, update);

            if(result.ModifiedCount > 0)
            {
                return true;
            }

            return false;
        }

        public async Task<TaskDTO> GetTaskByDescription(string owner, string description)
        {
            var filter = Builders<ListTask>.Filter.And(
                Builders<ListTask>.Filter.Eq(q => q.Owner, owner),
                Builders<ListTask>.Filter.ElemMatch(q => q.Tasks, q => q.Description == description)
            );

            var projection = Builders<ListTask>.Projection.Expression(list => list.Tasks
                .Where(q => q.Description == description)
                .ToList());

            var listTask = await _listTask.Find(filter).Project(projection).FirstOrDefaultAsync();

            if(listTask == null)
            {
                return null;
            }

            var newTask = listTask.FirstOrDefault();
            var taskDto = new TaskDTO
            {
                Description = newTask.Description,
                Category = newTask.Category,
                IsCompleted = newTask.IsCompleted,
                StartDate = newTask.StartDate.Date,
                EndDate = newTask.EndDate.Date
            };
            return taskDto;
        }
    }
}
