﻿using Klinika.Core;
using Klinika.Core.Database;
using Klinika.Rooms.Interfaces;
using Klinika.Rooms.Models;
using Klinika.Rooms.Services;
using System.Data;
using System.Data.SqlClient;


namespace Klinika.Rooms.Repositories
{
    internal class RoomRepository : Repository, IRoomRepo
    {
        public static Dictionary<int, string>? types { get; set; }
        public int GetTypeId(string type)
        {
            return types.FirstOrDefault(x => x.Value == type).Key;
        }
        public List<Room> Get()
        {
            string getQuery = "SELECT [Room].ID, [Room].Type, [Room].Number " +
                                      "FROM [Room] " +
                                      "WHERE IsDeleted = 0";
            var result = database.ExecuteSelectCommand(getQuery);
            var output = new List<Room>();
            foreach (object row in result)
            {
                var room = new Room(
                    id: Convert.ToInt32(((object[])row)[0].ToString()),
                    type: Convert.ToInt32(((object[])row)[1].ToString()),
                    number: Convert.ToInt32(((object[])row)[2].ToString()));
                output.Add(room);
            }
            return output;
        }
        public static DataTable GetAll()
        {
            GetAllTypes();
            return GetAllRoomsWithTypeNames();
        }
        public static DataTable GetAllRoomsWithTypeNames()
        {
            string getAllQuery = "SELECT [Room].ID, [RoomType].Name as Type, [Room].Number " +
                                      "FROM [Room] " +
                                      "INNER JOIN [RoomType] ON [Room].Type = [RoomType].ID " +
                                      "WHERE IsDeleted = 0";
            return DatabaseConnection.GetInstance().CreateTableOfData(getAllQuery);
        }
        public static void GetAllTypes()
        {
            types = new Dictionary<int, string>();
            DataTable? retrievedTypes = null;
            string getAllTypes = "SELECT ID, Name " +
                                      "FROM [RoomType] ";
            retrievedTypes = DatabaseConnection.GetInstance().CreateTableOfData(getAllTypes);
            foreach (DataRow row in retrievedTypes.Rows)
            {
                types.Add((int)row["ID"], row["Name"].ToString());
            }
        }

        public DataTable GetAllRooms()
        {
            string getAllQuery = "SELECT [Room].ID, [Room].Number " +
                                      "FROM [Room] " +
                                      "WHERE IsDeleted = 0";
            return DatabaseConnection.GetInstance().CreateTableOfData(getAllQuery);
        }

        public void Create(int type, int number)
        {
            string createQuery = "INSERT INTO [Room] " +
                "(Type, Number, IsDeleted) " +
                "VALUES (@Type, @Number, 0)";
            DatabaseConnection.GetInstance().ExecuteNonQueryCommand(createQuery, ("@Type", type), ("@Number", number));
        }

        public void Delete(int id)
        {
            string deleteRoom = "UPDATE [Room] SET IsDeleted = 1 WHERE ID = @ID";
            try
            {
                DatabaseConnection.GetInstance().database.Open();

                SqlCommand delete = new SqlCommand(deleteRoom, DatabaseConnection.GetInstance().database);
                delete.Parameters.AddWithValue("@ID", id);
                delete.ExecuteNonQuery();

                DatabaseConnection.GetInstance().database.Close();
            }
            catch (SqlException error)
            {
                MessageBox.Show(error.Message);
            }
        }

        internal void Modify(int id, string type, int number)
        {
            string modifyQuery = "UPDATE [Room] " +
                                 "SET Type = @Type, " +
                                 "Number = @Number " +
                                 "WHERE ID = @ID";
            DatabaseConnection.GetInstance().ExecuteNonQueryCommand(modifyQuery, ("@ID", id), ("@Type", GetTypeId(type)), ("@Number", number));
        }

        internal void SimpleRenovate(Renovation renovation)
        {
            string createQuery = "INSERT INTO [Renovations] " +
                "(RoomID, StartDate, EndDate, Advanced) " +
                "VALUES (@RoomID, @StartDate, @EndDate, @Advanced)";

            DatabaseConnection.GetInstance().ExecuteNonQueryCommand(createQuery, ("@RoomID", renovation.id),
                ("@StartDate", renovation.from.ToString("yyyy-MM-dd")), ("@EndDate", renovation.to.ToString("yyyy-MM-dd")),
                ("@Advanced", renovation.advanced));
        }

        internal void SplitRenovation(Renovation renovation, int renovationId)
        {
            string createQuery = "INSERT INTO [RenovationsAdvanced] " +
                "(ID, Type, FirstID, SecondID, SecondType, SecondNumber) " +
                "VALUES (@ID, @Type, @FirstID, @SecondID, @SecondType, @SecondNumber)";
            DatabaseConnection.GetInstance().ExecuteNonQueryCommand(createQuery, ("@ID", renovationId),
                ("@Type", 1), ("@FirstID", renovation.id), ("@SecondID", 0), ("@SecondType", renovation.secondType),
                ("@SecondNumber", renovation.secondNumber));
        }

        internal int FindRenovation(Renovation renovation)
        {
            string findQuery = "SELECT [Renovations].ID " +
                                      "FROM [Renovations] " +
                                      "WHERE RoomID = @RoomID AND StartDate = @StartDate AND EndDate = @EndDate";
            int id = (int)DatabaseConnection.GetInstance().ExecuteNonQueryScalarCommand(findQuery, ("@RoomID", renovation.id.ToString()),
                ("@StartDate", renovation.from.ToString("yyyy-MM-dd")), ("@EndDate", renovation.to.ToString("yyyy-MM-dd")));
            return id;
        }

        internal void MergeRenovation(Renovation renovation, int renovationId)
        {
            string createQuery = "INSERT INTO [RenovationsAdvanced] " +
                "(ID, Type, FirstID, SecondID, SecondType, SecondNumber) " +
                "VALUES (@ID, @Type, @FirstID, @SecondID, @SecondType, @SecondNumber)";
            DatabaseConnection.GetInstance().ExecuteNonQueryCommand(createQuery, ("@ID", renovationId), ("@Type", 0),
                ("@FirstID", renovation.id), ("@SecondID", renovation.secondId), ("@SecondType", 0), ("@SecondNumber", 0));
        }

        internal void AdvancedRenovation(Renovation renovation)
        {
            SimpleRenovate(renovation);
            if (renovation.advanced == 1)
            {
                MergeRenovation(renovation, FindRenovation(renovation));
                renovation.id = renovation.secondId;
                SimpleRenovate(renovation);
            }
            else if (renovation.advanced == 2)
            {
                SplitRenovation(renovation, FindRenovation(renovation));
            }
        }

        internal void MigrateEquipment(int firstId, int secondId)
        {
            string modifyQuery = "UPDATE [RoomEquipment] " +
                                 "SET RoomID = @first" +
                                 "WHERE RoomID = @second";

            DatabaseConnection.GetInstance().ExecuteNonQueryCommand(modifyQuery, ("@first", firstId), ("@second", secondId));
        }

        internal void MergeRooms(int firstId, int secondId)
        {
            RenovateRoom(firstId, secondId);
            MigrateEquipment(firstId, secondId);
            Delete(secondId);
        }

        internal void SplitRooms(int secondNumber, int secondType)
        {
            Create(secondNumber, secondType);
        }

        internal void RenovateRoom(int renovationID, int firstID)
        {
            string findQuery = "SELECT [RenovationsAdvanced].ID, [RenovationsAdvanced].SecondID, [RenovationsAdvanced].SecondType, [RenovationsAdvanced].SecondNumber " +
                                      "FROM [RenovationsAdvanced] " +
                                      "WHERE ID = @ID";
            DataTable advancedRenovation = DatabaseConnection.GetInstance().CreateTableOfData(findQuery, ("@ID", renovationID.ToString()));

            if (advancedRenovation.Rows.Count != 0)
            {
                if (advancedRenovation.Rows[0]["Type"].ToString() == "0")
                {
                    CheckRenovations();
                    MergeRooms(firstID, int.Parse(advancedRenovation.Rows[0]["SecondID"].ToString()));
                }
                if (advancedRenovation.Rows[0]["Type"].ToString() == "1")
                {
                    SplitRooms(int.Parse(advancedRenovation.Rows[0]["SecondNumber"].ToString()),
                        int.Parse(advancedRenovation.Rows[0]["SecondType"].ToString()));
                }
            }
        }

        public void CheckRenovations()
        {
            string findQuery = "SELECT [Renovations].ID, [Renovations].RoomID " +
                                      "FROM [Renovations] " +
                                      "WHERE EndDate = @Today AND Advanced != 0";
            DataTable renovations = DatabaseConnection.GetInstance().CreateTableOfData(findQuery, ("@Today", DateTime.Now.Date.ToString("yyyy-MM-dd")));
        }
    }
}