﻿
using Klinika.Roles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Klinika.Data;
using System.Data.SqlClient;

namespace Klinika.Repositories
{
    internal class UserRepository
    {
        public Dictionary<string, User> users { get; }
        public List<User> Users { get; }

        private static SqlConnection database = DatabaseConnection.GetInstance().database;

        private static UserRepository? singletonInstance;
        private UserRepository()
        {
            users = new Dictionary<string, User>();
            Users = new List<User>();
            string getCredentialsQuery = "SELECT [User].ID, [User].Name, [User].Surname, " +
                "Email, Password, UserType.Name as UserType, IsBlocked " +
                "FROM [User] " +
                "JOIN [UserType] ON [User].UserType = [UserType].ID " +
                "WHERE [User].IsDeleted = 0";
            try
            {
                SqlCommand getCredentials = new SqlCommand(getCredentialsQuery, database);
                database.Open();
                using (SqlDataReader retrieved = getCredentials.ExecuteReader())
                {
                    while (retrieved.Read())
                    {
                        int userID = Convert.ToInt32(retrieved["ID"]);
                        string name = retrieved["Name"].ToString();
                        string surname = retrieved["Surname"].ToString();
                        string email = retrieved["Email"].ToString();
                        string password = retrieved["Password"].ToString();
                        string userType = retrieved["UserType"].ToString();
                        bool isBlocked = Convert.ToBoolean(retrieved["IsBlocked"]);
                        User user = new User(userID,name,surname,email,password,userType,isBlocked);
                        users.TryAdd(user.Email, user);
                        Users.Add(user);
                    }
                }
                
            }
            catch (SqlException error)
            {
                MessageBox.Show(error.Message);
            }
            finally
            {
                database.Close();
            }

        }
        public static void Block(int ID)
        {
            string blockQuery = "UPDATE [User] SET " +
                "IsBlocked = @IsBlocked, " +
                "WhoBlocked = @WhoBlocked " +
                "WHERE ID = @ID";

            SqlCommand block = new SqlCommand(blockQuery, DatabaseConnection.GetInstance().database);
            block.Parameters.AddWithValue("@ID", ID);
            block.Parameters.AddWithValue("@IsBlocked", true);
            block.Parameters.AddWithValue("@WhoBlocked", "SYS");

            try
            {
                DatabaseConnection.GetInstance().database.Open();
                block.ExecuteNonQuery();
                DatabaseConnection.GetInstance().database.Close();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public static UserRepository GetInstance()
        {
            if (singletonInstance == null) singletonInstance = new UserRepository();
            return singletonInstance;
        }

    }
}
