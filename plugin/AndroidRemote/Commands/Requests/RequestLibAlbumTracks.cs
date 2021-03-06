﻿using MusicBeePlugin.AndroidRemote.Interfaces;

namespace MusicBeePlugin.AndroidRemote.Commands.Requests
{
    class RequestLibAlbumTracks : ICommand
    {
        public void Dispose()
        {
            
        }

        public void Execute(IEvent eEvent)
        {
            Plugin.Instance.LibraryGetAlbumTracks(eEvent.DataToString(), eEvent.ClientId);
        }
    }
}
