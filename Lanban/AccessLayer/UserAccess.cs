﻿using Lanban.Model;
using System;
using System.Data;
using System.Text;


namespace Lanban.AccessLayer
{
    public class UserAccess : Query
    {
        // Get User ID based on username
        public int getUserID(string username)
        {
            myCommand.CommandText = "SELECT TOP 1 User_ID FROM Users WHERE Username = @username";
            addParameter<string>("@username", SqlDbType.NVarChar, username);
            int result = Convert.ToInt32(myCommand.ExecuteScalar());
            myCommand.Parameters.Clear();
            return result;
        }

        // Get user data  based on username/id
        public UserModel getUserData<T>(T id)
        {
            dynamic uid = id;
            string type = uid.GetType().ToString();

            string command = "SELECT TOP 1 * FROM Users WHERE ";
            if (type.Contains("String"))
            {
                myCommand.CommandText = command + "Username = @username";
                addParameter<string>("@username", SqlDbType.NVarChar, uid);
            }
            else
            {
                myCommand.CommandText = command + "User_ID = @uid";
                addParameter<int>("@uid", SqlDbType.Int, uid);
            }
            
            UserModel user = null;
            myReader = myCommand.ExecuteReader();
            if (myReader.Read())
                user = SerializeTo<UserModel>(myReader);

            myReader.Close();
            myCommand.Parameters.Clear();
            return user;
        }

        //2.6.1 Save assignee/member of a task/backlog/project
        public void saveAssignee(string id, string type, string uid)
        {
            myCommand.CommandText = "INSERT INTO " + type + "_User (" + type + "_ID, User_ID) VALUES (@id, @uid)";
            addParameter<int>("@id", SqlDbType.Int, Convert.ToInt32(id));
            addParameter<int>("@uid", SqlDbType.Int, Convert.ToInt32(uid));
            myCommand.ExecuteNonQuery();
            myCommand.Parameters.Clear();
        }

        //2.6.2 Delete all assignee/member of a task/backlog/project
        public void deleteAssignee(string id, string type)
        {
            myCommand.CommandText = "DELETE FROM " + type + "_User WHERE " + type + "_ID=@id";
            addParameter<int>("@id", SqlDbType.Int, Convert.ToInt32(id));
            myCommand.ExecuteNonQuery();
            myCommand.Parameters.Clear();
        }

        //2.7 View assignee/member of a task/backlog
        public string viewAssignee(string id, string type)
        {
            myCommand.CommandText = "SELECT Users.User_ID, Users.[Name], Avatar FROM Users INNER JOIN " +
                "(SELECT User_ID FROM " + type + "_User WHERE " + type + "_ID=@id) AS A ON A.User_ID = Users.User_ID";
            addParameter<int>("@id", SqlDbType.Int, Convert.ToInt32(id));

            StringBuilder result = new StringBuilder();
            myReader = myCommand.ExecuteReader();
            while (myReader.Read())
            {
                string display = "<div class='assignee-name-active' data-id='" + myReader[0] + 
                    "' onclick='removeAssignee(this)'>" + myReader[1] + "</div>";
                result.Append(display);
            }
                
            myReader.Close();
            myCommand.Parameters.Clear();
            return result.ToString();
        }

        //a.1 Search member name in a project
        public string searchAssignee(int projectID, string keyword, string type)
        {
            myCommand.CommandText = "SELECT Users.User_ID, Name FROM Users INNER JOIN " +
                                    "(SELECT User_ID FROM Project_User WHERE Project_ID = " + projectID + ") AS A " +
                                    "ON A.User_ID = Users.User_ID WHERE Name LIKE '%" + keyword + "%'";
            StringBuilder result = new StringBuilder();

            myReader = myCommand.ExecuteReader();
            bool available = myReader.Read();
            if (available == false) result.Append("No record found.");
            else
            {
                while (available)
                {
                    string display = "<div class='resultline' data-id='" + myReader[0] + 
                        "' onclick=\"addAssignee(this,'" + type + "')\">" + myReader[1] + "</div>";
                    result.Append(display);
                    available = myReader.Read();
                }
            }
            myReader.Close();
            return result.ToString();
        }

        // 5.8 Search user based on name and role
        public string searchUser(string name, int role)
        {
            myCommand.CommandText = "SELECT TOP 3 User_ID, Name, Avatar FROM Users WHERE Role=@role AND Name LIKE '%" + name + "%'";
            addParameter<int>("@role", SqlDbType.Int, role);
            myReader = myCommand.ExecuteReader();
            StringBuilder result = new StringBuilder();
            while (myReader.Read())
            {
                result.Append("<div class='searchRecord' data-id='" + myReader["User_ID"] + "' ");
                result.Append("data-avatar='" + myReader["Avatar"] + "'>" + myReader["Name"] + "</div>");
            }
            if (result.ToString().Equals("")) return "No records found.";
            return result.ToString();
        }

        // 5.9.1 Save supervisor
        public void saveSupervisor(int projectID, int supervisorID)
        {
            myCommand.CommandText = "INSERT INTO Project_Supervisor (Project_ID, User_ID) VALUES (@projectID, @supervisorID)";
            addParameter<int>("@projectID", SqlDbType.Int, projectID);
            addParameter<int>("@supervisorID", SqlDbType.Int, supervisorID);
            myCommand.ExecuteNonQuery();
        }

        // 5.9.2 Edit supervisor
        public void deleteSupervisor(int projectID)
        {
            myCommand.CommandText = "DELETE FROM Project_Supervisor WHERE Project_ID = @projectID";
            addParameter<int>("@projectID", SqlDbType.Int, projectID);
            myCommand.ExecuteNonQuery();
        }

        // 5.7 A member quit project
        public bool quitProject(int projectID, int userID, int role)
        {
            string table = (role == 1) ? "Project_User" : "Project_Supervisor";
            myCommand.CommandText = "DELETE FROM " + table + " WHERE Project_ID=@projectID AND User_ID=@userID";
            addParameter<int>("@projectID", SqlDbType.Int, projectID);
            addParameter<int>("@userID", SqlDbType.Int, userID);
            bool result = (myCommand.ExecuteNonQuery() == 1);
            myCommand.Parameters.Clear();
            return result;
        }

        // Check whether a username is taken
        public string checkUsername(string username)
        {
            myCommand.CommandText = "IF EXISTS (SELECT Username FROM Users WHERE Username=@username) SELECT 1 ELSE SELECT 0";
            addParameter<string>("@username", SqlDbType.VarChar, username);
            if (Convert.ToInt32(myCommand.ExecuteScalar()) == 0) return "";
            return "Existed";
        }

        // Update avatar when user upload new avatar
        public void updateAvatar(int userID, string avatar)
        {
            myCommand.CommandText = "UPDATE Users SET Avatar=@avatar WHERE User_ID=@userID";
            addParameter<string>("@avatar", SqlDbType.NVarChar, avatar);
            addParameter<int>("@userID", SqlDbType.Int, userID);
            myCommand.ExecuteNonQuery();
        }
    }
}