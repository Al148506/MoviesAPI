﻿using System.Reflection.Metadata.Ecma335;

namespace MoviesAPI.DTOs
{
    public class ActorMovieCreationDTO
    {
        public int Id { get; set; }
        public required string Character { get; set; }
    }
}
