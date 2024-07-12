using final_backend.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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

                    // Hash the password before storing it
                    string hashedPassword = HashPassword(user.Password);

                    // Add parameters
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

        public List<Answers> GetAnswers()
        {
            List<Answers> answers = new List<Answers>();

            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new OracleCommand("PKG_SABA_QUESTIONS_AND_ANSWERS.get_answer_with_user", connection))
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
                            var answer = new Answers
                            {
                               
                                name_surname = reader["NAME_SURNAME"].ToString(),
                                question = reader["QUESTION"].ToString(),
                                answer = reader["ANSWER"].ToString(),
                            };
                            answers.Add(answer);
                        }
                    }
                }
            }

            return answers;
        }

        public void AddAnswers(List<Answers> answers)
        {
            try
            {
                using (var connection = GetConnection())
                {
                    connection.Open();
                    using (var command = new OracleCommand("PKG_SABA_QUESTIONS_AND_ANSWERS.add_answer", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Serialize the questions list to a JSON string
                        string answersJson = JsonSerializer.Serialize(answers);

                        // Log the JSON string for verification
                        Console.WriteLine(answersJson);

                        // Add the JSON string as a CLOB parameter
                        command.Parameters.Add("p_answer_json", OracleDbType.Clob).Value = answersJson;

                        command.ExecuteNonQuery();
                    }
                }

            }
            catch (Exception e)
            {
                throw new ApplicationException("Error adding questions", e);
            }
        }
        public void AddQuestion(Questions question)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new OracleCommand("PKG_SABA_QUESTIONS_AND_ANSWERS.add_question", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                  


                    // Add parameters
                    command.Parameters.Add("P_question", OracleDbType.Varchar2).Value = question.Question;
                    command.Parameters.Add("p_answer", OracleDbType.Varchar2).Value = question.Answer;
                    command.Parameters.Add("p_mandatory", OracleDbType.Varchar2).Value = question.Mandatory;
                    

                    command.ExecuteNonQuery();
                }
            }
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
