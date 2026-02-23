namespace SchoolEduERP.Data.Repositories;

public interface IUnitOfWork : IDisposable
{
    IStudentRepository Students { get; }
    ITeacherRepository Teachers { get; }
    IFeeHeadRepository FeeHeads { get; }
    IRepository<T> GetRepository<T>() where T : class;
    Task<int> SaveChangesAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IStudentRepository? _students;
    private ITeacherRepository? _teachers;
    private IFeeHeadRepository? _feeHeads;

    public UnitOfWork(ApplicationDbContext context) => _context = context;

    public IStudentRepository Students => _students ??= new StudentRepository(_context);
    public ITeacherRepository Teachers => _teachers ??= new TeacherRepository(_context);
    public IFeeHeadRepository FeeHeads => _feeHeads ??= new FeeHeadRepository(_context);

    public IRepository<T> GetRepository<T>() where T : class => new Repository<T>(_context);

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
