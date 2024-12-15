using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Faker;

namespace WindowsFormsApp1
{
    internal class Class1
    {
        public class Order
        {
            public DateTime Date { get; set; }
            public int Summ { get; set; }
        }
        public class Employee
        {
            public string Name { get; set; }
            public Order[] Orders { get; set; }
        }
        public static List<Employee> Generate(int num)
        {
            List<Employee> employees = new List<Employee>();
            for (int i = 0; i < num; i++)
            {
                int numOrders = Faker.RandomNumber.Next(50, 100);
                Order[] orders = new Order[numOrders];
                for(int j = 0; j < numOrders; j++)
                {
                    orders[j] = new Order {
                        Date = new DateTime(Faker.RandomNumber.Next(1733579793, 1734962193)),
                        Summ = Faker.RandomNumber.Next(1000, 100000)
                    };
                }
                employees.Add(new Employee
                {
                    Name = Faker.Name.First() + " " + Faker.Name.Last(),
                    Orders = orders
                });
            }
            return employees;
        }
    }
}
