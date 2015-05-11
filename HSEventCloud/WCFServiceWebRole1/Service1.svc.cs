using System;

using System.Collections.Generic;

using System.Linq;

using System.Runtime.Serialization;

using System.ServiceModel;

using System.ServiceModel.Web;

using System.Text;

//added for sql

using System.Data.SqlClient;

using System.Configuration;

using System.Data;

namespace WCFServiceWebRole1
{

    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.

    public class Service1 : IService1
    {
        public bool addProfile(string userID, string day, string month, string year, string eventName)
        {

            // code to add a new user profile to SQL database

            // creates a new instance of our database objects (user table in this case)

            using (var context = new HSChat_dbEntities())
            {

                // add a new object (or row) to our user profile table

                context.user_profile.Add(new user_profile()

               // bind each database column to the parameters we pass in our method

               // guid, firstname, surname, and email

                {

                    userID = userID,

                    day = day,

                    month = month,

                    year = year,

                    eventName = eventName

                });

                // commit changes to the user profile table

                context.SaveChanges();

                return true;

            }

        }

        public Users[] viewProfilesJSON(string userID)
        {

            // get the connections string stored in the web.fconfig file as a string

            string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            // create  new sql connections using the connection string

            SqlConnection thisConnection = new SqlConnection(connectionString);

            // create a new sql command called getUsers

            SqlCommand getUsers = thisConnection.CreateCommand();

            // create a temp list to store the rows of users returned from the database

            List<Users> users = new List<Users>();

            // open the sql connection and construct the select query

            thisConnection.Open();

            string sql = "select * from user_profile where userID=@userID";

            // paramertise your query to stop sql injections!

            getUsers.Parameters.AddWithValue("@userID", userID);

            getUsers.CommandText = sql;

            // create an sql data adapter using the getUsers query

            SqlDataAdapter da = new SqlDataAdapter(getUsers);

            // create a new dataset containing the rows returned from the user_profile table

            DataSet ds = new DataSet();

            da.Fill(ds, "user_profile");

            // for every row returned call our DataContract in IService1.cs

            foreach (DataRow dr in ds.Tables["user_profile"].Rows)
            {

                users.Add(new Users()

                {

                    EventID = Convert.ToInt32(dr[0]),

                    UserID = Convert.ToString(dr[1]),

                    Day = Convert.ToString(dr[2]),

                    Month = Convert.ToString(dr[3]),

                    Year = Convert.ToString(dr[4]),

                    EventName = Convert.ToString(dr[5])

                });

            }

            //Return data for JSON output

            thisConnection.Close();

            return users.ToArray();

        }
    
    }

}