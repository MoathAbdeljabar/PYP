using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Application.Identity.DTOs.Responses;
public class CreatedUserDto
{
    public string Id { get; set; }           // The client needs this for future requests
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

}

