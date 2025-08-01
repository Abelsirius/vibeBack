﻿namespace VibeME.DTOS
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public bool IsFollowing { get; set; } // True si el usuario logueado sigue a este usuario

    }
}
