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
        public int activeUsers = 0;
        public List<string> playerIDs;
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
        
        public void join(string player, string password)
        {
            lock (this)
            { 
                if (this.password == password)
                {
                    playerIDs.Add(player);
                    activeUsers++;
                }
            }
        }
        public void leave(string player)
        {
            lock(this)
            { 
                playerIDs.Remove(player);
                activeUsers--;
            }
        }
    }
}
