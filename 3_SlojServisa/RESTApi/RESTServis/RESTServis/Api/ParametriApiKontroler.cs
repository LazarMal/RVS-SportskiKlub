using System;
using System.Web.Http;
using RESTServis.Servisi;

namespace RESTServis.Controllers.Api
{
    [RoutePrefix("api/parametri")]
    public class ParametriController : ApiController
    {
        [HttpGet]
        [Route("poslovna-pravila")]
        public IHttpActionResult DajParametrePoslovnihPravila()
        {
            try
            {
                return Ok(ParametriPoslovnihPravilaDatoteka.Ucitaj());
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
