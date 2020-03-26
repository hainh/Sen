using System;
using System.Collections.Generic;

namespace Senla.Gamer
{
    public abstract class LobbyBase : Room
    {
        private List<Room> _rooms;

        /// <summary>
        /// Get a copy of List all current child <see cref="Room"/>s in this <see cref="LobbyBase"/>
        /// </summary>
        public List<Room> Rooms
        {
            get
            {
                lock (_rooms)
                {
                    var rooms = new List<Room>(_rooms);
                    return rooms;
                }
            }
        }

        public delegate void RoomChangedHandler(Room room);

        public event RoomChangedHandler RoomAdded;

        public event RoomChangedHandler RoomRemoved;

        public LobbyBase()
            : base(new PoolFiber(), short.MaxValue)
        {
            _rooms = new List<Room>(99);
            JoinLeaveBroadCastEnable = false;
            IsLobby = true;
        }

        public virtual Room FindRoom(string roomName)
        {
            if (this.RoomName == roomName)
            {
                return this;
            }

            foreach (var room in Rooms)
            {
                if (room.RoomName == roomName)
                {
                    return room;
                }

                if (room.IsLobby)
                {
                    var subRoom = ((LobbyBase)room).FindRoom(roomName);
                    if (subRoom != null)
                    {
                        return subRoom;
                    }
                }
            }

            return null;
        }

        protected void OnRoomAdded(Room room)
        {
            if (RoomAdded != null)
            {
                RoomAdded(room);
            }
        }

        protected void OnRoomRemoved(Room room)
        {
            if (RoomRemoved != null)
            {
                RoomRemoved(room);
            }
        }

        public virtual void AddNewRoom(Room room)
        {
            lock (_rooms)
            {
                _rooms.Add(room);
            }
            OnRoomAdded(room);
        }

        public virtual void RemoveRoom(Room room)
        {
            bool removed = false;
            lock (_rooms)
            {
                removed = _rooms.Remove(room);
            }

            if (removed)
            {
                OnRoomRemoved(room);
            }
        }

        /// <summary>
        /// Create new game room with request data from client.
        /// </summary>
        /// <param name="requestData">Data to create game</param>
        /// <returns>New game room or null if some criteria not meet (e.g. not enough money)</returns>
        public virtual Room CreateNewRoom(Dictionary<byte, object> requestData, Player player) { throw new NotImplementedException(); }
    }
}
