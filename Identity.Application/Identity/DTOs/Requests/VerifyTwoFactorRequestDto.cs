using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Application.Identity.DTOs.Requests;
    public class VerifyTwoFactorRequestDto
    {

    [Required]
    public string TempToken { get; set; }

    [Required]
    public string VerifyCode { get; set; }
    }

