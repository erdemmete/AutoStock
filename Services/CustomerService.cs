using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Services
{
    public class CustomerService(ICustomerRepository customerRepository): ICustomerService
    {
        
    }
}
