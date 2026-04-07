using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string City { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public int GoalsScored { get; set; }
    public int GoalsConceded { get; set; }
}

public class Game
{
    public int Id { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public int HomeGoals { get; set; }
    public int AwayGoals { get; set; }
    public Team HomeTeam { get; set; }
    public Team AwayTeam { get; set; }
}

public class ChampionshipContext : DbContext
{
    public DbSet<Team> Teams { get; set; }
    public DbSet<Game> Games { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ChampionshipDB;Trusted_Connection=True;");
    }
}

public class TeamService
{
    private readonly ChampionshipContext _context;
    public TeamService(ChampionshipContext context) { _context = context; }

    public async Task<Team> GetTeamByNameAsync(string name)
    {
        return await _context.Teams.FirstOrDefaultAsync(t => t.Name == name);
    }

    public async Task<List<Team>> GetTeamsByCityAsync(string city)
    {
        return await _context.Teams.Where(t => t.City == city).ToListAsync();
    }

    public async Task<Team> GetTeamByNameAndCityAsync(string name, string city)
    {
        return await _context.Teams.FirstOrDefaultAsync(t => t.Name == name && t.City == city);
    }
}

public class StatisticsService
{
    private readonly ChampionshipContext _context;
    public StatisticsService(ChampionshipContext context) { _context = context; }

    public async Task<Team> GetTeamWithMostWinsAsync()
    {
        return await _context.Teams.OrderByDescending(t => t.Wins).FirstOrDefaultAsync();
    }

    public async Task<Team> GetTeamWithMostLossesAsync()
    {
        return await _context.Teams.OrderByDescending(t => t.Losses).FirstOrDefaultAsync();
    }

    public async Task<Team> GetTeamWithMostDrawsAsync()
    {
        return await _context.Teams.OrderByDescending(t => t.Draws).FirstOrDefaultAsync();
    }

    public async Task<Team> GetTeamWithMostGoalsScoredAsync()
    {
        return await _context.Teams.OrderByDescending(t => t.GoalsScored).FirstOrDefaultAsync();
    }

    public async Task<Team> GetTeamWithMostGoalsConcededAsync()
    {
        return await _context.Teams.OrderByDescending(t => t.GoalsConceded).FirstOrDefaultAsync();
    }
}

public class TeamCrudService
{
    private readonly ChampionshipContext _context;
    public TeamCrudService(ChampionshipContext context) { _context = context; }

    public async Task<bool> AddTeamAsync(Team newTeam)
    {
        bool exists = await _context.Teams.AnyAsync(t => t.Name == newTeam.Name && t.City == newTeam.City);
        if (exists) { Console.WriteLine($"Команда {newTeam.Name} з міста {newTeam.City} вже існує!"); return false; }
        await _context.Teams.AddAsync(newTeam);
        await _context.SaveChangesAsync();
        Console.WriteLine($"Команду {newTeam.Name} додано успішно!");
        return true;
    }

    public async Task<bool> UpdateTeamAsync(string name, string city, Action<Team> updateAction)
    {
        var team = await _context.Teams.FirstOrDefaultAsync(t => t.Name == name && t.City == city);
        if (team == null) { Console.WriteLine($"Команду {name} з міста {city} не знайдено."); return false; }
        updateAction(team);
        await _context.SaveChangesAsync();
        Console.WriteLine($"Дані команди {name} оновлено.");
        return true;
    }

    public async Task<bool> DeleteTeamAsync(string name, string city)
    {
        var team = await _context.Teams.FirstOrDefaultAsync(t => t.Name == name && t.City == city);
        if (team == null) { Console.WriteLine($"Команду {name} з міста {city} не знайдено."); return false; }
        Console.Write($"Ви дійсно хочете видалити команду '{team.Name}' з міста {team.City}? (так/ні): ");
        var answer = Console.ReadLine();
        if (answer?.ToLower() == "так") { _context.Teams.Remove(team); await _context.SaveChangesAsync(); Console.WriteLine($"Команду {name} видалено."); return true; }
        Console.WriteLine("Видалення скасовано.");
        return false;
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ChampionshipContext>();
        services.AddScoped<TeamService>();
        services.AddScoped<StatisticsService>();
        services.AddScoped<TeamCrudService>();
        var provider = services.BuildServiceProvider();

        var teamService = provider.GetRequiredService<TeamService>();
        var statsService = provider.GetRequiredService<StatisticsService>();
        var crudService = provider.GetRequiredService<TeamCrudService>();

        Console.WriteLine("=== ЗАВДАННЯ 1: ПОШУК ===");
        var team = await teamService.GetTeamByNameAsync("Реал Мадрид");
        if (team != null) Console.WriteLine($"Знайдено: {team.Name} - {team.City}");

        var teamsInCity = await teamService.GetTeamsByCityAsync("Мадрид");
        Console.WriteLine($"Команд у Мадриді: {teamsInCity.Count}");

        var exactTeam = await teamService.GetTeamByNameAndCityAsync("Барселона", "Барселона");
        if (exactTeam != null) Console.WriteLine($"Точно знайдено: {exactTeam.Name}");

        Console.WriteLine("\n=== ЗАВДАННЯ 2: СТАТИСТИКА ===");
        var mostWins = await statsService.GetTeamWithMostWinsAsync();
        Console.WriteLine($"Найбільше перемог: {mostWins?.Name} - {mostWins?.Wins}");

        var mostLosses = await statsService.GetTeamWithMostLossesAsync();
        Console.WriteLine($"Найбільше поразок: {mostLosses?.Name} - {mostLosses?.Losses}");

        var mostDraws = await statsService.GetTeamWithMostDrawsAsync();
        Console.WriteLine($"Найбільше нічиїх: {mostDraws?.Name} - {mostDraws?.Draws}");

        var mostScored = await statsService.GetTeamWithMostGoalsScoredAsync();
        Console.WriteLine($"Найбільше забито: {mostScored?.Name} - {mostScored?.GoalsScored}");

        var mostConceded = await statsService.GetTeamWithMostGoalsConcededAsync();
        Console.WriteLine($"Найбільше пропущено: {mostConceded?.Name} - {mostConceded?.GoalsConceded}");

        Console.WriteLine("\n=== ЗАВДАННЯ 3: CRUD ===");
        var newTeam = new Team { Name = "Жирона", City = "Жирона", Wins = 0, Losses = 0, Draws = 0, GoalsScored = 0, GoalsConceded = 0 };
        await crudService.AddTeamAsync(newTeam);

        await crudService.UpdateTeamAsync("Жирона", "Жирона", t =>
        {
            t.Wins = 8;
            t.Losses = 5;
            t.Draws = 4;
            t.GoalsScored = 28;
            t.GoalsConceded = 22;
        });

        await crudService.DeleteTeamAsync("Жирона", "Жирона");
    }
}