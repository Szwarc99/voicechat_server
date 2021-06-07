using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryServer2
{
    class Room
    {
        public Guid id;
        private string password;
        public bool begun;
        public int activeUsers;
        public int[] playerID;
        public bool isPrivate;
        public Room(bool isPrivate, string password)
        {
            this.isPrivate = isPrivate;
            this.password = password;
            this.id = new Guid();
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
