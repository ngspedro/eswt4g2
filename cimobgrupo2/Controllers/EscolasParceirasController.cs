﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using cimobgrupo2.Models;
using cimobgrupo2.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace cimobgrupo2.Controllers
{
    public class EscolasParceirasController : Controller
    {
        private ApplicationDbContext _context;
        private readonly List<Ajuda> _ajudas;
        private readonly List<Erro> _erros;
        private List<EscolaParceira> _escolasParceiras;

        public EscolasParceirasController(ApplicationDbContext context)
        {
            _context = context;
            _ajudas = context.Ajudas.Where(ai => ai.Controller == "EscolasParceiras").ToList();
            _erros = context.Erros.ToList();
            _escolasParceiras = context.EscolasParceiras.Include(e => e.Cursos).ThenInclude(e => e.Curso).ToList();
        }

        public IActionResult Index()
        {
            return View(ProperView("Index"), _escolasParceiras);
        }

        public IActionResult Detalhes(int? id)
        {
            return View(ProperView("Detalhes"), _escolasParceiras.Find(e => e.EscolaParceiraId == id));
        }

        public IActionResult Adicionar()
        {
            FillCountryList();
            return View(ProperView("Adicionar"));
        }

        [HttpPost]
        public IActionResult Adicionar([Bind("EscolaParceiraId,Nome,Pais,Localidade")] EscolaParceira EscolaParceira)
        {
            if (ModelState.IsValid)
            {
                _context.Add(EscolaParceira);
                _context.SaveChanges();
                SetSuccessMessage("Escola Parceira adicionada.");
                return RedirectToAction(nameof(Index));
            }
            SetErrorMessage("003");
            return View(EscolaParceira);
        }

        public IActionResult Editar(int? Id)
        {
            FillCountryList();
            return View(ProperView("Editar"), _context.EscolasParceiras.SingleOrDefault(e => e.EscolaParceiraId == Id));
        }

        [HttpPost]
        public IActionResult Editar([Bind("EscolaParceiraId,Nome,Pais,Localidade")] EscolaParceira EscolaParceira)
        {
            if (ModelState.IsValid)
            {
                EscolaParceira atual = _context.EscolasParceiras.SingleOrDefault(e => e.EscolaParceiraId == EscolaParceira.EscolaParceiraId);
                atual.Nome = EscolaParceira.Nome;
                atual.Pais = EscolaParceira.Pais;
                atual.Localidade = EscolaParceira.Localidade;
                _context.SaveChanges();
                SetSuccessMessage("Escola Parceira editada.");
                return RedirectToAction(nameof(Index));
            }
     
            SetErrorMessage("003");
            return View(EscolaParceira);
        }

        public IActionResult RemoverModal(int? Id)
        {
            return PartialView(ProperView("RemoverModal"), _context.EscolasParceiras.SingleOrDefault(c => c.EscolaParceiraId == Id));
        }

        public IActionResult Remover(int EscolaParceiraId)
        {
            _context.EscolasParceiras.Remove(_context.EscolasParceiras.SingleOrDefault(e => e.EscolaParceiraId == EscolaParceiraId));
            _context.SaveChanges();
            SetSuccessMessage("Escola parceira removida.");
            return RedirectToAction(nameof(Index));
        }

        public String ProperView(String viewName)
        {
            if (User.IsInRole("CIMOB"))
                return "~/Views/EscolasParceiras/Cimob/" + viewName + ".cshtml";

            return viewName;
        }

        private void SetErrorMessage(String Code)
        {
            var Erro = _erros.SingleOrDefault(e => e.Codigo == Code);
            TempData["Error_Code"] = Erro.Codigo;
            TempData["Error_Message"] = Erro.Mensagem;
        }

        private void SetSuccessMessage(String Message)
        {
            TempData["Success"] = Message;
        }

        private void FillCountryList()
        {
            List<string> CountryList = new List<string>();
            CultureInfo[] CInfoList = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
            foreach (CultureInfo CInfo in CInfoList)
            {
                RegionInfo R = new RegionInfo(CInfo.LCID);
                if (!(CountryList.Contains(R.DisplayName)))
                {
                    CountryList.Add(R.DisplayName);
                }
            }

            CountryList.Sort();
            ViewBag.CountryList = CountryList;
        }
    }
}