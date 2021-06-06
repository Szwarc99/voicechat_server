using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryServer2
{
    class Room
    {
        public int id;
        private string password;
        public bool begun;
        public int activeUsers;
        public int[] playerID;
        public bool isPrivate;
        public Room(int id, bool isPrivate, string password)
        {
            this.isPrivate = isPrivate;
            this.password = password;
            this.id = id;
        }
        public string Password
        {
            get { return password; }
            set { password = ""; }
        }
        public void join()
        {
            activeUsers++;
        }
        public void leave()
        {
            activeUsers--;
        }
    }
}
