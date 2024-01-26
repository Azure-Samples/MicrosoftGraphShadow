//using Azure.Identity;
//using Microsoft.Graph;
//using Microsoft.Extensions.Options;
//using Common.Models;
//using Microsoft.IdentityModel.Tokens;
//using Microsoft.Graph.Models;
//using Microsoft.Kiota.Abstractions;
//using System.Collections;
//using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
//using Microsoft.Graph.Applications.Delta;


//namespace Services.Graph
//{



//    public interface IGraphDevelopmentService : IGraphService
//    {
//        Task CreateTestUser(string t, int usersToCreate);
//        Task CreateTestGroup(string t);
//        Task<int> DeleteTestUsers(string t);
//        Task<int> DeleteTestGroup(string t);
//    }
//    public partial class GraphService : IGraphDevelopmentService
//    {
//        private static string CreateRandomPasswordWithRandomLength()
//        {
//            string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-";
//            Random random = new Random();

//            int size = random.Next(20, validChars.Length);
//            char[] chars = new char[size];
//            for (int i = 0; i < size; i++)
//            {
//                chars[i] = validChars[random.Next(0, validChars.Length)];
//            }
//            return new string(chars);
//        }
//        public async Task<int> DeleteTestUsers(string t)
//        {
//            var client = GetGraphServiceClient(t);
//            int count = 0;
//            var r = await GetUserPagesAsync(t, "city eq 'Test'");
//            foreach (var user in r.Where(w => w.City == "Test"))
//            {
//                await client.Users[user.Id].DeleteAsync();
//                count++;
//            }
//            return count;
//        }

//        public async Task CreateTestUser(string t, int usersToCreate)
//        {
//            var client = GetGraphServiceClient(t);

//            var batchRequestContentCollection = new BatchRequestContentCollection(client);


//            List<RequestInformation> ri = new();
//            for (int i = 1; i < usersToCreate; i++)
//            {
//                var temp = Guid.NewGuid().ToString().Replace("-", "");

//                var bi = client.Users.ToPostRequestInformation(new User { DisplayName = Guid.NewGuid().ToString().Split('-')[0], UserPrincipalName = temp + "@hwhfta.onmicrosoft.com", City = "Test", AccountEnabled = true, MailNickname = temp, PasswordProfile = new PasswordProfile { ForceChangePasswordNextSignIn = true, Password = CreateRandomPasswordWithRandomLength() } });
                
//                await batchRequestContentCollection.AddBatchRequestStepAsync(bi);
//                if (i % 5 == 0)
//                {
//                    try
//                    {
//                        var batchResponse = await client.Batch.PostAsync(batchRequestContentCollection);
//                        var responseCodes = await batchResponse.GetResponsesStatusCodesAsync();
//                        var codes = responseCodes.Where(w => w.Value != System.Net.HttpStatusCode.Created);
//                        batchRequestContentCollection = new BatchRequestContentCollection(client);
//                    }
//                    catch (Exception e)
//                    {
//                        var rrrr = e.Message;
//                        throw;
//                    }
//                }
//            }
//        }


//        public async Task UpdateTestGroup(string t)
//        {
//            try
//            {
//                var client = GetGraphServiceClient(t);
//                var g = await GetGroupsAsync(t);
//                var updateGroup= g.Where(w => w.Description == "DeleteMe").FirstOrDefault();
//                updateGroup.DisplayName = Guid.NewGuid().ToString().Split('-')[0];
                
//                var r = await client.Groups[updateGroup.Id].PatchAsync(updateGroup);

//            }
//            catch (Exception)
//            {

//                throw;
//            }
//        }



//        public async Task UpdateTestUser(string t)
//        {
//            try
//            {
//                var client = GetGraphServiceClient(t);
//                var g = await GetUsersAsync(t, "id eq '709cb747-2ec2-4dce-a087-a20d30b78599'");
//                var u = g.First();
//                u.DisplayName = Guid.NewGuid().ToString().Split('-')[0];

//                var r = await client.Users[u.Id].PatchAsync(u);

//            }
//            catch (Exception)
//            {

//                throw;
//            }
//        }


//        public async Task CreateTestGroup(string t)
//        {
//            try
//            {
//                var client = GetGraphServiceClient(t);
//                var id = Guid.NewGuid().ToString().Split('-')[0];
//                var g = new Group { DisplayName = id, Description = "DeleteMe", MailEnabled=false, MailNickname=id, SecurityEnabled=true };
//                var r = await client.Groups.PostAsync(g);
                
//                //var batchRequestContentCollection = new BatchRequestContentCollection(client);


//                //List<RequestInformation> ri = new();
//                //for (int i = 0; i < usersToCreate; i++)
//                //{
//                //    var temp = Guid.NewGuid().ToString().Replace("-", "");

//                //    var bi = client.Groups.ToPostRequestInformation(new Group { DisplayName = Guid.NewGuid().ToString().Split('-')[0],  Description = "DeleteMe" });

//                //    await batchRequestContentCollection.AddBatchRequestStepAsync(bi);
//                //}
//                //var batchResponse = await client.Batch.PostAsync(batchRequestContentCollection);
//                //var responseCodes = await batchResponse.GetResponsesStatusCodesAsync();

//            }
//            catch (Exception)
//            {

//                throw;
//            }
//        }
//        public async Task<int> DeleteTestGroup(string t)
//        {
//            var client = GetGraphServiceClient(t);
//            int count = 0;
//            var r = await GetGroupsAsync(t);
//            foreach (var user in r!.Where(w => w.Description == "DeleteMe"))
//            {
//                await client.Groups[user.Id].DeleteAsync();
//                count++;
//            }
//            return count;
//        }
//    }
//}
