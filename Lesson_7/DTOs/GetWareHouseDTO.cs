using System.ComponentModel.DataAnnotations;

namespace Lesson_7.DTOs;

public record GetWareHouseDTO(
    [Required]int IdProduct, 
    [Required]int IdWarehouse, 
    [Required]int Amount, 
    [Required]DateTime CreatedTime
    );
