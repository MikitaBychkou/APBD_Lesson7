using System.Data;
using System.Data.SqlClient;
using Lesson_7.DTOs;

namespace Lesson_7.Services;

public interface IDbService
{
    Task<int> GetIdProductWarehouse(GetWareHouseDTO wareHouseDto);
}

public class DbService(IConfiguration configuration) : IDbService
{
    private async Task<SqlConnection> GetConnection()
    {
        var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return connection;
    }

    public async Task<int> GetIdProductWarehouse(GetWareHouseDTO wareHouseDto)
    {
        await using var connection = await GetConnection();
        var com = new SqlCommand();
        com.Connection = connection;
        com.CommandText = """
                               SELECT COUNT(*) FROM Product WHERE IdProduct = @ID
                               """;
        com.Parameters.AddWithValue("@ID", wareHouseDto.IdProduct);
        var result = (int)(await com.ExecuteScalarAsync())!;
        if (result == 0)
        {
            throw new ArgumentException("This Produc doesn't exist.");
        }
        
        var com1 = new SqlCommand();
        com1.Connection = connection;
        com1.CommandText = """
                              SELECT COUNT(*) FROM Warehouse WHERE IdWarehouse = @ID
                              """;
        com1.Parameters.AddWithValue("@ID", wareHouseDto.IdWarehouse);
        var result1 = (int)(await com1.ExecuteScalarAsync())!;
        if (result1 == 0)
        {
            throw new ArgumentException("This Warehouse doesn't exist.");
        }

        if (!(wareHouseDto.Amount > 0))
        {
            throw new ArgumentException("Your Amount mast be greater than 0.");
        }
        
        var com2 = new SqlCommand();
        com2.Connection = connection;
        com2.CommandText = """
                               SELECT IdOrder FROM [Order] WHERE IdProduct = @id and Amount = @amount and CreatedAt < @createdAt
                               """;
        com2.Parameters.AddWithValue("@id", wareHouseDto.IdProduct);
        com2.Parameters.AddWithValue("@amount", wareHouseDto.Amount);
        com2.Parameters.AddWithValue("@createdAt", wareHouseDto.CreatedTime);
        
        var reader=await com2.ExecuteReaderAsync();
        
        if (!reader.HasRows)
        {
            throw new ArgumentException("This Order isn't corredt.");
        }
        await reader.ReadAsync();
        var tmpId =reader.GetInt32(0);
        
        await reader.DisposeAsync();
        
        var com3 = new SqlCommand();
        com3.Connection = connection;
        com3.CommandText = """
                               SELECT count(*) from Product_Warehouse WHERE IdOrder = @ID
                               """;
        com3.Parameters.AddWithValue("@ID", tmpId);
        var result2 = (int)(await com3.ExecuteScalarAsync())!;
        if (result2 != 0)
        {
            throw new ArgumentException("This Order has already been fulfilled.");
        }
        
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var com4 = new SqlCommand();
            com4.Connection = connection;
            com4.CommandText = """
                               UPDATE [Order] Set FulfilledAt = @p1 WHERE IdOrder = @p2
                               """;
            com4.Transaction = (SqlTransaction)transaction;

            com4.Parameters.AddWithValue("@p1", DateTime.Now);
            com4.Parameters.AddWithValue("@p2", tmpId);

            await com4.ExecuteNonQueryAsync();
            
            var com5 = new SqlCommand();

            com5.Connection = connection;
            com5.CommandText = """
                               SELECT Price*@amount FROM Product WHERE IdProduct = @idProduct
                               """;
            com5.Transaction = (SqlTransaction)transaction;
            com5.Parameters.AddWithValue("@amount", wareHouseDto.Amount);
            com5.Parameters.AddWithValue("@idProduct", wareHouseDto.IdProduct);
            
            var price = (await com5.ExecuteScalarAsync())!;
            var resultPrice = Convert.ToDouble(price);
            
            

            var com6 = new SqlCommand();

            com6.Connection = connection;
            com6.CommandText = """
                               INSERT INTO Product_Warehouse VALUES (@p1,@p2,@p3,@p4,@p5,@p6);
                               SELECT CAST(scope_identity() as int)
                               """;
            com6.Transaction = (SqlTransaction)transaction;
            
            com6.Parameters.AddWithValue("@p1", wareHouseDto.IdWarehouse);
            com6.Parameters.AddWithValue("@p2", wareHouseDto.IdProduct);
            com6.Parameters.AddWithValue("@p3", tmpId);
            com6.Parameters.AddWithValue("@p4", wareHouseDto.Amount);
            com6.Parameters.AddWithValue("@p5", resultPrice);
            com6.Parameters.AddWithValue("@p6", DateTime.Now);
            
            var IdToRetern = (int)(await com6.ExecuteScalarAsync())!;
            
            await transaction.CommitAsync();
            
            return IdToRetern;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        } 
    }
}