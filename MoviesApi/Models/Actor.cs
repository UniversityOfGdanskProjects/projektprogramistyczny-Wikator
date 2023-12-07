﻿namespace MoviesApi.Models;

public class Actor
{
    public int Id { get; set; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string DateOfBirth { get; init; }
    public string? Biography { get; init; }
    
    public ICollection<Movie>? Movies { get; init; }
}