using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using Exam.Models;

namespace Exam.Controllers
{
    public class RecyclableController : Controller
    {
        public ActionResult Index()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            string query = "SELECT Id, Type FROM RecyclableType";

            List<SelectListItem> recyclableTypes = new List<SelectListItem>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            recyclableTypes.Add(new SelectListItem
                            {
                                Value = reader["Id"].ToString(),
                                Text = reader["Type"].ToString()
                            });
                        }
                    }
                }
            }

            ViewBag.RecyclableTypes = recyclableTypes;

            return View();
        }


        [HttpPost]
        public ActionResult SaveRecyclableType(string Type, decimal Rate, decimal MinKg, decimal MaxKg)
        {
            if (ModelState.IsValid)
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                string query = "INSERT INTO RecyclableType (Type, Rate, MinKg, MaxKg) VALUES (@Type, @Rate, @MinKg, @MaxKg)";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Type", Type);
                        command.Parameters.AddWithValue("@Rate", Rate);
                        command.Parameters.AddWithValue("@MinKg", MinKg);
                        command.Parameters.AddWithValue("@MaxKg", MaxKg);

                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }

                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpPost]
        public ActionResult SaveRecyclableItem(int recyclableType, string description, decimal weight)
        {
            if (recyclableType <= 0 || weight <= 0 || string.IsNullOrEmpty(description))
            {
                ModelState.AddModelError("", "Please fill out all required fields.");
                return RedirectToAction("Index");
            }

       
            decimal rate = 0;
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            string rateQuery = "SELECT Rate FROM RecyclableType WHERE Id = @RecyclableTypeId";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(rateQuery, connection))
                {
                    command.Parameters.AddWithValue("@RecyclableTypeId", recyclableType);
                    connection.Open();
                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        rate = Convert.ToDecimal(result);
                    }
                    connection.Close();
                }
            }

            decimal computedRate = Math.Round(rate * weight, 2); 

            string insertQuery = "INSERT INTO RecyclableItem (RecyclableTypeId, ItemDescription, Weight, ComputedRate) " +
                                 "VALUES (@RecyclableTypeId, @ItemDescription, @Weight, @ComputedRate)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@RecyclableTypeId", recyclableType);
                    command.Parameters.AddWithValue("@ItemDescription", description);
                    command.Parameters.AddWithValue("@Weight", weight);
                    command.Parameters.AddWithValue("@ComputedRate", computedRate);

                    connection.Open();
                    command.ExecuteNonQuery(); 
                    connection.Close();
                }
            }

            return RedirectToAction("Index");
        }
        [HttpPost]
        public JsonResult ComputeRate(int recyclableTypeId, decimal weight)
        {
            decimal rate = 0;
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            string query = "SELECT Rate FROM RecyclableType WHERE Id = @RecyclableTypeId";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@RecyclableTypeId", recyclableTypeId);
                connection.Open();

                var result = command.ExecuteScalar();
                if (result != null)
                {
                    rate = Convert.ToDecimal(result);
                }
            }

            decimal computedRate = Math.Round(rate * weight, 2);

            return Json(new { computedRate = computedRate.ToString("F2") });
        }

        public JsonResult ValidateWeight(int recyclableType, decimal weight)
        {
            decimal minKg = 0;
            decimal maxKg = 0;

            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            string query = "SELECT MinKg, MaxKg FROM RecyclableType WHERE Id = @RecyclableTypeId";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@RecyclableTypeId", recyclableType);
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            minKg = Convert.ToDecimal(reader["MinKg"]);
                            maxKg = Convert.ToDecimal(reader["MaxKg"]);
                        }
                        else
                        {
                            return Json(new { isValid = false, message = "Invalid recyclable type." }, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
            }

            // Check if weight is within the valid range
            if (weight < minKg || weight > maxKg)
            {
                return Json(new { isValid = false, message = $"Weight must be between {minKg} and {maxKg}." }, JsonRequestBehavior.AllowGet);
            }

            // If the weight is valid
            return Json(new { isValid = true }, JsonRequestBehavior.AllowGet);
        }


    }
}
