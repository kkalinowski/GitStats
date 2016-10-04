module Types

open System

type AuthorStats = 
    { Name : string
      FilesModifications : int
      AddedFiles : int
      RemovedFiles : int
      RenamedFiles : int
      CommitsCount : int
      CommitsPerDay : double }

type DateStats = 
    { Date : DateTime
      Count : int }

type DayOfWeekStats = 
    { Day : DayOfWeek
      Count : int }

type HourStats = 
    { Hour : int
      Count : int }

type ExtensionStats = 
    { Extension : string
      Count : int }

type DirectoryStats = 
    { FilesCount : int
      DirectoriesCount : int
      ExtensionStats : ExtensionStats seq }

type RepoStats = 
    { CommitsCount : int
      CommitsPerDay : double
      BranchesCount : int
      TagsCount : int
      DaysFromLastCommit : int
      AuthorStats : AuthorStats seq
      DateStats : DateStats seq
      DayOfWeekStats : DayOfWeekStats seq
      HourStats : HourStats seq
      DirectoryStats : DirectoryStats }