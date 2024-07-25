using final_backend.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Result = final_backend.Models.Result;

namespace final_backend.Packages
{

    public class Pkg_users : Pkg_base
    {
        public Pkg_users(string connectionString) : base(connectionString)
        {


        }
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public void RegisterUser(User user)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new OracleCommand("PKG_SABA_USERS.register_user", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                   
                    string hashedPassword = HashPassword(user.Password);

                   
                    command.Parameters.Add("p_username", OracleDbType.Varchar2).Value = user.UserName;
                    command.Parameters.Add("p_email", OracleDbType.Varchar2).Value = user.Name;
                    command.Parameters.Add("p_password", OracleDbType.Varchar2).Value = hashedPassword;
                    command.Parameters.Add("p_user_role", OracleDbType.Int32).Value = user.UserRole;

                    command.ExecuteNonQuery();
                }
            }
        }
        public List<User> GetUsers()
        {
            List<User> users = new List<User>();

            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new OracleCommand("PKG_SABA_USERS.get_users", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    OracleParameter cursorParameter = new OracleParameter();
                    cursorParameter.OracleDbType = OracleDbType.RefCursor;
                    cursorParameter.Direction = ParameterDirection.Output;
                    command.Parameters.Add(cursorParameter);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var user = new User
                            {
                                Id = Convert.ToInt32(reader["ID"]),
                                UserName = reader["USERNAME"].ToString(),
                                Name = reader["NAME"].ToString(),
                                UserRole = Convert.ToInt32(reader["USERROLE"])
                            };
                            users.Add(user);
                        }
                    }
                }
            }

            return users;
        }
        public User LoginUser(string username, string password)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new OracleCommand("PKG_SABA_USERS.login_user", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add("p_username", OracleDbType.Varchar2).Value = username;

                    OracleParameter cursorParameter = new OracleParameter();
                    cursorParameter.OracleDbType = OracleDbType.RefCursor;
                    cursorParameter.Direction = ParameterDirection.Output;
                    command.Parameters.Add(cursorParameter);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var dbPassword = reader["PASSWORD"].ToString();
                            var user = new User
                            {
                                Id = Convert.ToInt32(reader["ID"]),
                                UserName = reader["USERNAME"].ToString(),
                                Name = reader["NAME"].ToString(),
                                UserRole = Convert.ToInt32(reader["USERROLE"]),
                                // Password = dbPassword
                                // Add other properties as needed
                            };

                            // Validate password
                            if (ValidatePassword(password, dbPassword))
                            {
                                return user;
                            }
                        }
                    }
                }
            }

            throw new UnauthorizedAccessException("Invalid email or password");
        }
        public User GetUser(int userId)
        {
            User user = null;

            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new OracleCommand("PKG_SABA_USERS.get_user", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add("p_id", OracleDbType.Int32).Value = userId;

                    OracleParameter cursorParameter = new OracleParameter();
                    cursorParameter.OracleDbType = OracleDbType.RefCursor;
                    cursorParameter.Direction = ParameterDirection.Output;
                    command.Parameters.Add(cursorParameter);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = new User
                            {
                                Id = Convert.ToInt32(reader["ID"]),
                                UserName = reader["USERNAME"].ToString(),
                                Name = reader["EMAIL"].ToString(),
                                UserRole = Convert.ToInt32(reader["USERROLE"])
                                // Add other properties as needed
                            };
                        }
                    }
                }
            }

            return user;
        }

        public List<Result> GetResults(int userId)
        {
            var results = new List<Result>();

            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new OracleCommand("PKG_SABA_QUESTIONS_AND_ANSWERS.get_result", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add("p_id", OracleDbType.Int32).Value = userId;

                    OracleParameter cursorParameter = new OracleParameter();
                    cursorParameter.OracleDbType = OracleDbType.RefCursor;
                    cursorParameter.Direction = ParameterDirection.Output;
                    command.Parameters.Add(cursorParameter);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new Result
                            {
                                question = reader["QUESTION"].ToString(),
                                answer = reader["ANSWER"].ToString(),
                                name_id = Convert.ToInt32(reader["NAME_ID"]),
                            });
                        }
                    }
                }
            }

            return results;
        }
        public List<Person> GetNames()
        {
            var names = new List<Person>();

            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new OracleCommand("PKG_SABA_QUESTIONS_AND_ANSWERS.get_names", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Define the output parameter for the cursor
                    OracleParameter cursorParameter = new OracleParameter();
                    cursorParameter.OracleDbType = OracleDbType.RefCursor;
                    cursorParameter.Direction = ParameterDirection.Output;
                    command.Parameters.Add(cursorParameter);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            names.Add(new Person
                            {
                                Name = reader["NAME"].ToString(),
                                Id = Convert.ToInt32(reader["ID"])
                            });
                        }
                    }
                }
            }

            return names;
        }

        public void AddResult(string json)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new OracleCommand("PKG_SABA_QUESTIONS_AND_ANSWERS.add_result", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add the JSON parameter
                    OracleParameter jsonParameter = new OracleParameter();
                    jsonParameter.ParameterName = "p_json";
                    jsonParameter.OracleDbType = OracleDbType.Clob;
                    jsonParameter.Direction = ParameterDirection.Input;
                    jsonParameter.Value = json;
                    command.Parameters.Add(jsonParameter);

                    // Execute the stored procedure
                    command.ExecuteNonQuery();
                }
            }
        }
        public List<Questions> AddQuestion(Questions question)
        {
            var questionsList = new List<Questions>();

            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new OracleCommand("PKG_SABA_QUESTIONS_AND_ANSWERS.add_question", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    command.Parameters.Add("p_question", OracleDbType.Varchar2).Value = question.Question;
                    command.Parameters.Add("p_answer", OracleDbType.Varchar2).Value = question.Answer;
                    command.Parameters.Add("p_is_mandatory", OracleDbType.Int32).Value = question.Mandatory;

                    // Add cursor parameter
                    OracleParameter cursorParameter = new OracleParameter();
                    cursorParameter.OracleDbType = OracleDbType.RefCursor;
                    cursorParameter.Direction = ParameterDirection.Output;
                    command.Parameters.Add(cursorParameter);

                    // Execute the command and process the cursor
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var q = new Questions
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                Question = reader["question"].ToString(),
                                Answer = reader["answer"].ToString(),
                                Mandatory = Convert.ToInt32(reader["mandatory"])
                            };
                            questionsList.Add(q);
                        }
                    }
                }
            }

            return questionsList;
        }

        public List<Questions> GetQuestions()
        {
            List<Questions> questions = new List<Questions>();

            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new OracleCommand("PKG_SABA_QUESTIONS_AND_ANSWERS.get_questions", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    OracleParameter cursorParameter = new OracleParameter();
                    cursorParameter.OracleDbType = OracleDbType.RefCursor;
                    cursorParameter.Direction = ParameterDirection.Output;
                    command.Parameters.Add(cursorParameter);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var question = new Questions
                            {

                                 Id = Convert.ToInt32(reader["ID"]),
                                Question = reader["QUESTION"].ToString(),
                                Answer = reader["ANSWER"].ToString(),
                                Mandatory = Convert.ToInt32(reader["MANDATORY"]),
                            };
                            questions.Add(question);
                        }
                    }
                }
            }

            return questions;
        }
        public void EditQuestion(int id, Questions question)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new OracleCommand("PKG_SABA_QUESTIONS_AND_ANSWERS.edit_question", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add("p_id", OracleDbType.Int32).Value = id;
                    command.Parameters.Add("p_question", OracleDbType.Varchar2).Value = question.Question;
                    command.Parameters.Add("p_answer", OracleDbType.Varchar2).Value = question.Answer;
                    command.Parameters.Add("p_is_mandatory", OracleDbType.Int32).Value = question.Mandatory;
                  

                    command.ExecuteNonQuery();
                }
            }
        }





        private bool ValidatePassword(string inputPassword, string dbPasswordHash)
        {
            // Hash input password and compare with dbPasswordHash
            string hashedInputPassword = HashPassword(inputPassword);
            return hashedInputPassword == dbPasswordHash;
        }
    }
}
