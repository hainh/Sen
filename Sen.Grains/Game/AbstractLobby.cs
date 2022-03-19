﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sen
{
    public delegate void RoomChanged(IRoom room);

    /// <inheritdoc/>
    public abstract class AbstractLobby<TGrainState> : AbstractRoom<TGrainState>, ILobby where TGrainState : ILobbyState
    {
        public event RoomChanged RoomAdded;

        public event RoomChanged RoomRemoved;

        protected IList<IRoom> _rooms;

        public AbstractLobby()
        {
            //State.PlayerLimit = int.MaxValue;
        }

        public ValueTask<IList<IRoom>> GetRooms() => new(_rooms);

        public override ValueTask<bool> IsLobby() => new(true);

        public async ValueTask<IRoom> FindRoom(string roomName)
        {
            foreach (IRoom room in _rooms)
            {
                if (await room.GetRoomName() == roomName)
                {
                    return room;
                }
                if (await room.IsLobby())
                {
                    var found = await ((ILobby)room).FindRoom(roomName);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }

        protected void OnRoomAdded(IRoom room)
        {
            RoomAdded?.Invoke(room);
        }

        protected void OnRoomRemoved(IRoom room)
        {
            RoomRemoved?.Invoke(room);
        }

        public async ValueTask AddNewRoom(IRoom room)
        {
            _rooms.Add(room);
            await room.SetParent(this);
            OnRoomAdded(room);
        }

        public async ValueTask<bool> RemoveRoom(IRoom room)
        {
            bool removed = _rooms.Remove(room);
            if (removed)
            {
                await room.SetParent(null);
                OnRoomRemoved(room);
            }
            return removed;
        }
    }
}
