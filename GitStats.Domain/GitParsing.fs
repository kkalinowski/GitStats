module GitParsing

open LibGit2Sharp
open System
open System.IO

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

let getCommits pathToRepo = 
    let repo = new Repository(pathToRepo)
    let filter = new CommitFilter()
    repo.Commits.QueryBy filter, repo

let getDateStats (commits : Commit seq) = 
    commits
    |> Seq.groupBy (fun x -> x.Author.When.LocalDateTime.Date)
    |> Seq.map (fun (date, commits) -> 
           { Date = date
             Count = (Seq.length commits) })

let addMissingDaysOfWeek (dayStats : DayOfWeekStats seq) = 
    Enum.GetValues(typeof<DayOfWeek>)
    |> Seq.cast<DayOfWeek>
    |> Seq.map (fun x -> 
           let validStat = Seq.tryFind (fun y -> y.Day = x) dayStats
           match validStat with
           | Some stat -> 
               { Day = x
                 Count = stat.Count }
           | None -> 
               { Day = x
                 Count = 0 })

let getDayOfWeekStats (commits : Commit seq) = 
    commits
    |> Seq.groupBy (fun x -> x.Author.When.LocalDateTime.Date.DayOfWeek)
    |> Seq.map (fun (day, commits) -> 
           { Day = day
             Count = (Seq.length commits) })
    |> addMissingDaysOfWeek
    |> Seq.sortBy (fun x -> ((int x.Day) + 6) % 7)

let addMissingHours (hourStats : HourStats seq) = 
    [ 0..23 ] |> Seq.map (fun x -> 
                     let validStat = Seq.tryFind (fun y -> y.Hour = x) hourStats
                     match validStat with
                     | Some stat -> 
                         { Hour = x
                           Count = stat.Count }
                     | None -> 
                         { Hour = x
                           Count = 0 })

let getHourStats (commits : Commit seq) = 
    commits
    |> Seq.groupBy (fun x -> x.Author.When.LocalDateTime.Hour)
    |> Seq.map (fun (hour, commits) -> 
           { Hour = hour
             Count = (Seq.length commits) })
    |> addMissingHours
    |> Seq.sortBy (fun x -> x.Hour)

let getDaysFromLastCommit (commits : Commit seq) = 
    let lastCommit = 
        commits
        |> Seq.map (fun x -> x.Author.When.LocalDateTime.Date)
        |> Seq.max
    int (DateTime.Today.Subtract lastCommit).TotalDays

let getRepoDaysSpan (commits : Commit seq) = 
    let firstCommit = 
        commits
        |> Seq.map (fun x -> x.Author.When.LocalDateTime.Date)
        |> Seq.min
    
    let lastCommit = 
        commits
        |> Seq.map (fun x -> x.Author.When.LocalDateTime.Date)
        |> Seq.max
    
    (lastCommit - firstCommit).TotalDays

let getCommitsPerDay daysSpan (commits : Commit seq) = (double (Seq.length commits)) / daysSpan

let getRepoTreeForDiff (commits : Commit seq) = 
    commits
    |> Seq.pairwise
    |> Seq.rev
    |> Seq.append [ Seq.last commits, null ]

let getCommitDiff (repo : Repository) (toDiff : Commit * Commit) = 
    let newCommit = fst toDiff
    
    let oldTree = 
        if snd toDiff = null then null
        else (snd toDiff).Tree
    
    let diff = repo.Diff.Compare<TreeChanges>(oldTree, newCommit.Tree)
    newCommit, diff

let computeAuthorStats daysSpan (commits : (Commit * TreeChanges) seq) = 
    commits
    |> Seq.groupBy (fun x -> (fst x).Author.Name)
    |> Seq.map (fun (name, commitAndChange) -> (name, Seq.map snd commitAndChange))
    |> Seq.map (fun (name, changes) -> 
           { Name = name
             CommitsCount = (Seq.length changes)
             FilesModifications = changes |> Seq.sumBy (fun x -> Seq.length x.Modified)
             AddedFiles = changes |> Seq.sumBy (fun x -> Seq.length x.Added)
             RemovedFiles = changes |> Seq.sumBy (fun x -> Seq.length x.Deleted)
             RenamedFiles = changes |> Seq.sumBy (fun x -> Seq.length x.Renamed)
             CommitsPerDay = double (Seq.length changes) / double daysSpan })
    |> Seq.sortBy (fun x -> x.Name)

let computeExtensionStats (files : FileInfo seq) = 
    files
    |> Seq.countBy (fun x -> x.Extension)
    |> Seq.map (fun x -> 
           { Extension = fst x
             Count = snd x })
    |> Seq.sortByDescending (fun x -> x.Count)

let getDirectoryStats pathToRepo = 
    let files = 
        Directory.EnumerateFiles(pathToRepo, "*.*", SearchOption.AllDirectories)
        |> Seq.map (fun x -> new FileInfo(x))
        |> Seq.filter (fun x -> not (x.Attributes.HasFlag FileAttributes.Hidden))
        |> Seq.filter (fun x -> not (x.Extension = ""))
    
    let directories = 
        Directory.EnumerateDirectories(pathToRepo, "*.*", SearchOption.AllDirectories)
        |> Seq.map (fun x -> new DirectoryInfo(x))
        |> Seq.filter (fun x -> not (x.Attributes.HasFlag FileAttributes.Hidden))
    
    { FilesCount = files |> Seq.length
      DirectoriesCount = directories |> Seq.length
      ExtensionStats = files |> computeExtensionStats }

let getBranchesCount (repo : Repository) = Seq.length repo.Branches
let getTagsCount (repo : Repository) = Seq.length repo.Tags

let getRepoStats pathToRepo = 
    let commits, repo = getCommits pathToRepo
    let daysSpan = getRepoDaysSpan commits
    
    let getAuthorStats = 
        getRepoTreeForDiff
        >> Seq.map (getCommitDiff repo)
        >> computeAuthorStats daysSpan
    { CommitsCount = Seq.length commits
      CommitsPerDay = getCommitsPerDay daysSpan commits
      BranchesCount = getBranchesCount repo
      TagsCount = getTagsCount repo
      DaysFromLastCommit = getDaysFromLastCommit commits
      AuthorStats = getAuthorStats commits
      DateStats = getDateStats commits
      DayOfWeekStats = getDayOfWeekStats commits
      HourStats = getHourStats commits
      DirectoryStats = getDirectoryStats pathToRepo }
