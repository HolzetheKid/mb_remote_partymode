﻿using System;
using System.Runtime.Serialization;

namespace MusicBeePlugin.AndroidRemote.Entities
{
    [DataContract]

    class Track :IEquatable<Track>, IComparable<Track>
    {
        public Track()
        {
            
        }

        public Track(string artist, string title, int trackNo, string src)
        {
            this.Artist = artist;
            this.Title = title;
            this.Src = src;
            this.Trackno = trackNo;
        }

        [DataMember(Name = "src")]
        public string Src { get; set; }

        [DataMember(Name = "artist")]
        public string Artist { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "trackno")]
        public int Trackno { get; set; }

        [DataMember(Name = "disc")]
        public int Disc { get; set; }

        [DataMember(Name = "album")]
        public string Album { get; set; }
         
        [DataMember(Name = "album_artist")]
        public string AlbumArtist { get; set; }

        [DataMember(Name = "genre")]
        public string Genre { get; set; }

        public bool Equals(Track other)
        {
            return (other.Artist.Equals(Artist) && other.Title.Equals(Title));
        }

        public int CompareTo(Track other)
        {
            return other == null ? 1 : Trackno.CompareTo(other.Trackno);
        }
    }
}
