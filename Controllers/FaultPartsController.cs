﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KollamAutoEng_web.Areas.Identity.Data;
using KollamAutoEng_web.Models;
using Microsoft.AspNetCore.Authorization;
using KollamAutoEng_web.Migrations;
using Microsoft.Data.SqlClient;
using NuGet.Protocol.Plugins;

namespace KollamAutoEng_web.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class FaultPartsController : Controller
    {
        private readonly KollamAutoEng_webContext _context;

        public FaultPartsController(KollamAutoEng_webContext context)
        {
            _context = context;
        }

        // GET: FaultParts
        [Authorize(Roles = "Admin,Employee")] // Restricts access to users with the Admin or Employee role
        public async Task<IActionResult> Index(string sortOrder, string searchString, string currentFilter, int? pageNumber)
        {
            // Set up sorting parameters for customer sorting
            ViewData["CustomerSortParm"] = sortOrder == "Customer" ? "customer_desc" : "Customer";

            // Reset the page number if a new search is performed
            if (searchString != null)
            {
                pageNumber = 1; // Reset to the first page when a new search is made
            }
            else
            {
                // Retain the current filter string for pagination
                searchString = currentFilter;
            }

            // Check if the FaultPart context is null
            if (_context.FaultPart == null)
            {
                return Problem("Entity set 'KollamAutoEng_webContext.FaultPart' is null."); // Return an error if it is
            }

            // Store the current search string for use in the view
            ViewData["CurrentFilter"] = searchString;

            // Query to retrieve fault parts, including related entities
            var faultparts = from faultp in _context.FaultPart
                             .Include(m => m.Fault)
                             .Include(m => m.Part)
                             .Include(m => m.Appointment)
                             .Include(m => m.Customer)
                             .Include(m => m.Vehicle)
                             select faultp;

            // If the search string is not empty, filter the fault parts based on various fields
            if (!String.IsNullOrEmpty(searchString))
            {
                faultparts = faultparts.Where(m =>
                    m.Fault.FaultName.Contains(searchString) || // Search by fault name
                    m.Part.PartName.Contains(searchString) || // Search by part name
                    m.Appointment.AppointmentName.Contains(searchString) || // Search by appointment name
                    m.Vehicle.Registration.Contains(searchString) || // Search by vehicle registration
                    m.Customer.FirstName.Contains(searchString) || // Search by customer's first name
                    m.Customer.LastName.Contains(searchString) || // Search by customer's last name
                    (m.Customer.FirstName + " " + m.Customer.LastName).Contains(searchString) // Search by full customer name
                );
            }

            // Sort fault parts based on the selected sort order
            switch (sortOrder)
            {
                case "Customer":
                    faultparts = faultparts.OrderBy(s => s.Customer.FirstName).ThenBy(s => s.Customer.LastName); // Ascending order
                    break;
                case "customer_desc":
                    faultparts = faultparts.OrderByDescending(s => s.Customer.FirstName).ThenByDescending(s => s.Customer.LastName); // Descending order
                    break;
            }

            int pageSize = 5; // Define the number of items per page
                              // Return the paginated list of fault parts to the view
            return View(await PaginatedList<FaultPart>.CreateAsync(faultparts.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: FaultParts/Details
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.FaultPart == null)
            {
                return NotFound();
            }

            var faultPart = await _context.FaultPart
                .Include(f => f.Appointment)
                .Include(f => f.Fault)
                .Include(f => f.Part)
                .Include(f => f.Customer)
                .Include(f => f.Vehicle)
                .FirstOrDefaultAsync(m => m.FaultPartId == id);
            if (faultPart == null)
            {
                return NotFound();
            }

            return View(faultPart);
        }

        // GET: FaultParts/Create
        [Authorize(Roles = "Admin,Employee")]
        public IActionResult Create()
        {
            ViewData["AppointmentId"] = new SelectList(_context.Appointment, "AppointmentId", "AppointmentName");
            ViewData["FaultId"] = new SelectList(_context.Fault, "FaultId", "FaultName");
            ViewData["PartId"] = new SelectList(_context.Part, "PartId", "PartName");
            ViewData["CustomerId"] = new SelectList(_context.Customer, "CustomerId", "FirstName");
            ViewData["VehicleId"] = new SelectList(_context.Vehicle, "VehicleId", "Registration");
            return View();
        }

        // POST: FaultParts/Create
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FaultPartId,FaultId,PartId,AppointmentId,CustomerId,VehicleId")] FaultPart faultPart)
        {
            if (ModelState.IsValid)
            {
                _context.Add(faultPart);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AppointmentId"] = new SelectList(_context.Appointment, "AppointmentId", "AppointmentName", faultPart.AppointmentId);
            ViewData["FaultId"] = new SelectList(_context.Fault, "FaultId", "FaultName", faultPart.FaultId);
            ViewData["PartId"] = new SelectList(_context.Part, "PartId", "PartName", faultPart.PartId);
            ViewData["CustomerId"] = new SelectList(_context.Customer, "CustomerId", "FirstName", faultPart.CustomerId);
            ViewData["VehicleId"] = new SelectList(_context.Vehicle, "VehicleId", "Registration", faultPart.VehicleId);
            return View(faultPart);
        }

        // GET: FaultParts/Edit
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.FaultPart == null)
            {
                return NotFound();
            }

            var faultPart = await _context.FaultPart.FindAsync(id);
            if (faultPart == null)
            {
                return NotFound();
            }
            ViewData["AppointmentId"] = new SelectList(_context.Appointment, "AppointmentId", "AppointmentName", faultPart.AppointmentId);
            ViewData["FaultId"] = new SelectList(_context.Fault, "FaultId", "FaultName", faultPart.FaultId);
            ViewData["PartId"] = new SelectList(_context.Part, "PartId", "PartName", faultPart.PartId);
            ViewData["CustomerId"] = new SelectList(_context.Customer, "CustomerId", "FirstName", faultPart.CustomerId);
            ViewData["VehicleId"] = new SelectList(_context.Vehicle, "VehicleId", "Registration", faultPart.VehicleId);
            return View(faultPart);
        }

        // POST: FaultParts/Edit
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FaultPartId,FaultId,PartId,AppointmentId,CustomerId,VehicleId")] FaultPart faultPart)
        {
            if (id != faultPart.FaultPartId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(faultPart);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FaultPartExists(faultPart.FaultPartId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AppointmentId"] = new SelectList(_context.Appointment, "AppointmentId", "AppointmentName", faultPart.AppointmentId);
            ViewData["FaultId"] = new SelectList(_context.Fault, "FaultId", "FaultName", faultPart.FaultId);
            ViewData["PartId"] = new SelectList(_context.Part, "PartId", "PartName", faultPart.PartId);
            ViewData["CustomerId"] = new SelectList(_context.Customer, "CustomerId", "FirstName", faultPart.CustomerId);
            ViewData["VehicleId"] = new SelectList(_context.Vehicle, "VehicleId", "Registration", faultPart.VehicleId);
            return View(faultPart);
        }

        // GET: FaultParts/Delete
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.FaultPart == null)
            {
                return NotFound();
            }

            var faultPart = await _context.FaultPart
                .Include(f => f.Appointment)
                .Include(f => f.Fault)
                .Include(f => f.Part)
                .Include(f => f.Customer)
                .Include(f => f.Vehicle)
                .FirstOrDefaultAsync(m => m.FaultPartId == id);
            if (faultPart == null)
            {
                return NotFound();
            }

            return View(faultPart);
        }

        // POST: FaultParts/Delete
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin,Employee")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.FaultPart == null)
            {
                return Problem("Entity set 'KollamAutoEng_webContext.FaultPart'  is null.");
            }
            var faultPart = await _context.FaultPart.FindAsync(id);
            if (faultPart != null)
            {
                _context.FaultPart.Remove(faultPart);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FaultPartExists(int id)
        {
          return (_context.FaultPart?.Any(e => e.FaultPartId == id)).GetValueOrDefault();
        }
    }
}
