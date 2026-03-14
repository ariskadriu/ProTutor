using System.Collections.Generic;

namespace ProgrammingTutor.Models
{
    public class Challenge
    {
        public string Title { get; set; } = string.Empty;
        public string Exercise { get; set; } = string.Empty;
        public string ExpectedOutput { get; set; } = string.Empty;
        public string Solution { get; set; } = string.Empty;
        public string Difficulty { get; set; } = "Easy"; // Easy, Medium, Hard, Expert, Mastery
        public List<string> RequiredKeywords { get; set; } = new List<string>();
    }

    public class Lesson
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;
        public List<Challenge> Challenges { get; set; } = new List<Challenge>();
    }
}
