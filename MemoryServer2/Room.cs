using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryServer2
{
    class Room
    {
        private Guid id;
        private string password;
        public bool begun;
        public int activeUsers;
        public bool isPrivate;
        public Room(bool isPrivate, string password)
        {
            this.id = Guid.NewGuid();
            this.isPrivate = isPrivate;
            this.password = password;
        }
        public Guid getID()
        {
            return this.id;
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
