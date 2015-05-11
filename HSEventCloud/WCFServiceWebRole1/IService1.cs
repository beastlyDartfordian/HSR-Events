using System;

using System.Collections.Generic;

using System.Linq;

using System.Runtime.Serialization;

using System.ServiceModel;

using System.ServiceModel.Web;

using System.Text;

namespace WCFServiceWebRole1
{

    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.

    [ServiceContract]

    public interface IService1
    {
        // create a new URI template that contains a URI endpoint that our mobile application will call through a async webclient

        // the URI will contain parameters to pass in a guid, firstname, and surname

        // when the URI ic called the parameters are passed into our addProfile() method which is then called in Service1.svc

        [OperationContract]

        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest,

        UriTemplate = "users?userID={userID}&day={day}&month={month}&year={year}&eventName={eventName}")]

        bool addProfile(string userID, string day, string month, string year, string eventName);

        [OperationContract]

        [WebInvoke(Method = "GET",

        ResponseFormat = WebMessageFormat.Json,

        BodyStyle = WebMessageBodyStyle.Bare,

        UriTemplate = "viewusers?format=json&userID={userID}")]

        Users[] viewProfilesJSON(string userID);

    }

    [DataContract]

    public class Users
    {

        [DataMember]

        private int eventID;

        public int EventID
        {

            get { return eventID; }

            set { eventID = value; }

        }

        [DataMember]

        private string userID;

        public string UserID
        {

            get { return userID; }

            set { userID = value; }

        }

        [DataMember]

        private string day;

        public string Day
        {

            get { return day; }

            set { day = value; }

        }

        [DataMember]

        private string month;

        public string Month
        {

            get { return month; }

            set { month = value; }

        }

        [DataMember]

        private string year;

        public string Year
        {

            get { return year; }

            set { year = value; }

        }

        [DataMember]

        private string eventName;

        public string EventName
        {

            get { return eventName; }

            set { eventName = value; }

        }

    }

}