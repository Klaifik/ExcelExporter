using ExcelExporter.Data;
using ExcelExporter.Models;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

public interface IExcelFileProcessor : IDisposable
{
    void DuplicateFile(string sourceFile, string outputFile);
}

public class NpoiExcelFileProcessor : IExcelFileProcessor
{
    private bool _disposed = false;

    public void DuplicateFile(string sourceFile, string outputFile)
    {
        if (!File.Exists(sourceFile))
        {
            throw new FileNotFoundException($"Сурс не найден: {sourceFile}", sourceFile);
        }

        try
        {
            using (var inputStream = File.OpenRead(sourceFile))
            using (var outputStream = File.Create(outputFile))
            {
                IWorkbook workbook = new XSSFWorkbook(inputStream);
                workbook.Write(outputStream);
            }
        }
        catch (IOException ex)
        {
            throw new ExcelProcessingException($"Ошибка ввода-вывода: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new ExcelProcessingException($"Ошибка: {ex.Message}", ex);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~NpoiExcelFileProcessor()
    {
        Dispose(false);
    }
}

public class ExcelProcessingException : Exception
{
    public ExcelProcessingException(string message, Exception innerException) : base(message, innerException) { }
}

public class ExcelDuplicator : IDisposable
{
    private readonly IExcelFileProcessor _excelProcessor;
    private bool _disposed = false;

    public ExcelDuplicator(IExcelFileProcessor excelProcessor)
    {
        _excelProcessor = excelProcessor ?? throw new ArgumentNullException(nameof(excelProcessor));
    }

    public void Duplicate(IEnumerable<Model> models, string sourceFile, string outputFile)
    {
        ValidateInput(sourceFile, outputFile);

        foreach (var model in models)
        {
            string outputFileName = $"[{model.Id}]-[{model.Name}].xlsx";
            string outputFilePath = Path.Combine(Path.GetDirectoryName(outputFile), outputFileName);
            _excelProcessor.DuplicateFile(sourceFile, outputFilePath);

            using (var stream = new FileStream(outputFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                IWorkbook workbook = new XSSFWorkbook(stream);
                ISheet sheet = workbook.GetSheetAt(0);


                IRow row = sheet.CreateRow(sheet.LastRowNum + 1);
                row.CreateCell(0).SetCellValue(model.Id);
                row.CreateCell(1).SetCellValue(model.Name);


                workbook.Write(stream);
            }
        }
    }

    private void ValidateInput(string sourceFile, string outputFile)
    {
        if (string.IsNullOrWhiteSpace(sourceFile)) throw new ArgumentNullException(nameof(sourceFile));
        if (string.IsNullOrWhiteSpace(outputFile)) throw new ArgumentNullException(nameof(outputFile));
        if (!File.Exists(sourceFile)) throw new FileNotFoundException("Инпутного файла не существует.", sourceFile);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _excelProcessor?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~ExcelDuplicator()
    {
        Dispose(false);
    }
}

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        using (var dbContext = new MyDbContext())
        {
            dbContext.Database.EnsureCreated();

            if (!dbContext.Models.Any())
            {
                dbContext.Models.Add(new Model { Name = "Запись 1" });
                dbContext.Models.Add(new Model { Name = "Запись 2" });
                dbContext.SaveChanges();
            }
        }

        string sourceFile = "Data\\input.xlsx";

        using (var saveFileDialog = new SaveFileDialog())
        {
            saveFileDialog.Filter = "Excel файлы (*.xlsx)|*.xlsx";
            saveFileDialog.Title = "Сохранить файл как";
            saveFileDialog.FileName = "output.xlsx";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string outputFile = saveFileDialog.FileName;

                List<Model> models;

                using (var dbContext = new MyDbContext())
                {
                    models = dbContext.Models.ToList();
                }

                try
                {
                    using (var processor = new NpoiExcelFileProcessor())
                    using (var duplicator = new ExcelDuplicator(processor))
                    {
                        duplicator.Duplicate(models, sourceFile, outputFile);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Фатальная ошибка: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Выбор файла отменен.");
            }
        }
    }
}