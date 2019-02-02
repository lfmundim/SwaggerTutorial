using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Blip.HttpClient.Factories;
using Lime.Messaging.Resources;
using Lime.Protocol;
using Microsoft.AspNetCore.Mvc;
using RestEase;

namespace SwaggerTraining.Controllers
{
    /// <summary>
    /// Controller responsible for handling Contacts
    /// </summary>
    [Route("[controller]"), Produces("application/json")]
    public class ContactsController : Controller
    {
        private BlipHttpClientFactory _factory;

        public ContactsController()
        {
            _factory = new BlipHttpClientFactory();
        }

        /// <summary>
        /// Gets all contacts from a bot's contact list
        /// </summary>
        /// <param name="authorization">The Authorization (Key) for the requested bot</param>
        /// <param name="sampleEnum">Sample enum to serve as an example</param>
        /// <remarks>
        /// Sample request:
        ///     
        ///     GET /contacts HTTP/1.1
        ///     Host: localhost:XXXX
        ///     Authorization: Key YmFneTpzMDVUZGlvNmZLV2t5TDl6Njl4Wg=
        ///     
        /// </remarks>
        /// <response code="200">Successfully retrieved the bot's contacts</response>
        /// <response code="401">Missing or incomplete <paramref name="authorization"/> header</response>
        /// <response code="500">Not mapped exception. See <c>Exception</c> object thrown for details.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<Contact>), 200)]
        public async Task<IActionResult> GetAllAsync([FromHeader(Name = "Authorization"), Required] string authorization, [FromQuery] HttpStatusCode sampleEnum)
        {
            try
            {
                CheckAuthorization(authorization);

                var client = _factory.BuildBlipHttpClient(authorization);
                var command = new Command
                {
                    Method = CommandMethod.Get,
                    Uri = new LimeUri("/contacts")
                };
                var response = await client.ProcessCommandAsync(command, CancellationToken.None);

                if (response.Status == CommandStatus.Success)
                {
                    var contactCollection = response.Resource as DocumentCollection;
                    if (contactCollection == null)
                    {
                        throw new Exception("Could not get contacts from BLiP", new Exception(response.Reason.Description));
                    }

                    return StatusCode((int)HttpStatusCode.OK, contactCollection.Items.ToList());
                }
                else
                {
                    throw new ApiException(HttpMethod.Get, new Uri("/contacts"), HttpStatusCode.BadRequest,
                        $"Failed to get contacts from bot's agenda: {response.Reason.Description}", null, null, "");
                }
            }
            catch (AuthenticationException authex)
            {
                return StatusCode((int)HttpStatusCode.Unauthorized, authex);
            }
            catch (ApiException apiex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, apiex);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Gets a given <c>Contact</c> from a bot's contact list
        /// </summary>
        /// <param name="authorization">The Authorization (Key) for the requested bot</param>
        /// <param name="identity">The desired contact's <c>identity</c></param>
        /// <remarks>
        /// Sample request:
        ///     
        ///     GET /contacts/useridentity.chatbot@0mn.io HTTP/1.1
        ///     Host: localhost:XXXX
        ///     Authorization: Key YmFneTpzMDVUZGlvNmZLV2t5TDl6Njl4Wg=
        ///     
        /// </remarks>
        /// <response code="200">Successfully retrieved the <paramref name="identity"/>'s <c>Contact</c></response>
        /// <response code="401">Missing or incomplete <paramref name="authorization"/> header</response>
        /// <response code="500">Not mapped exception. See <c>Exception</c> object thrown for details.</response>
        [HttpGet("{identity}")]
        [ProducesResponseType(typeof(Contact), 200)]
        public async Task<IActionResult> GetAsync([FromHeader(Name = "Authorization"), Required] string authorization, string identity)
        {
            try
            {
                CheckAuthorization(authorization);

                var client = _factory.BuildBlipHttpClient(authorization);
                var command = new Command
                {
                    Method = CommandMethod.Get,
                    Uri = new LimeUri($"/contacts/{identity}")
                };
                var response = await client.ProcessCommandAsync(command, CancellationToken.None);

                if (response.Status == CommandStatus.Success)
                {
                    var contact = response.Resource as Contact;
                    if (contact == null)
                    {
                        throw new Exception($"Could not get contact from BLiP using {identity}", new Exception(response.Reason.Description));
                    }

                    return StatusCode((int)HttpStatusCode.OK, contact);
                }
                else
                {
                    throw new ApiException(HttpMethod.Get, new Uri("/contacts/{identity}"), HttpStatusCode.BadRequest,
                        $"Failed to get contact from bot's agenda using {identity}: {response.Reason.Description}", null, null, "");
                }
            }
            catch (AuthenticationException authex)
            {
                return StatusCode((int)HttpStatusCode.Unauthorized, authex);
            }
            catch (ApiException apiex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, apiex);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Adds a <c>Contact</c> to a bot's contact list
        /// </summary>
        /// <param name="authorization">The Authorization (Key) for the requested bot</param>
        /// <param name="contact">The contact to add to the bot's agenda</param>
        /// <remarks>
        /// Sample request:
        ///     
        ///     POST /contacts HTTP/1.1
        ///     Host: localhost:XXXX
        ///     Authorization: Key YmFneTpzMDVUZGlvNmZLV2t5TDl6Njl4Wg=
        /// 
        ///     {  
        ///         "identity": "11121023102013021@messenger.gw.msging.net",
        ///         "name": "John Doe",
        ///         "gender":"male",
        ///         "group":"friends",    
        ///         "extras": {
        ///             "plan":"Gold",
        ///             "code":"1111"      
        ///         }
        ///     }
        /// </remarks>
        /// <response code="201">Successfully added the <paramref name="contact"/> to the bot's agenda</response>
        /// <response code="401">Missing or incomplete <paramref name="authorization"/> header</response>
        /// <response code="500">Not mapped exception. See <c>Exception</c> object thrown for details.</response>
        [HttpPost]
        [ProducesResponseType(typeof(Command), 201)]
        public async Task<IActionResult> PostAsync([FromHeader(Name = "Authorization"), Required] string authorization, [FromBody] Contact contact)
        {
            try
            {
                CheckAuthorization(authorization);

                var client = _factory.BuildBlipHttpClient(authorization);
                var command = new Command
                {
                    Method = CommandMethod.Set,
                    Uri = new LimeUri($"/contacts"),
                    Resource = contact
                };
                var response = await client.ProcessCommandAsync(command, CancellationToken.None);

                if (response.Status == CommandStatus.Success)
                {
                    return StatusCode((int)HttpStatusCode.Created, response);
                }
                else
                {
                    throw new ApiException(HttpMethod.Get, new Uri("/contacts"), HttpStatusCode.BadRequest,
                        $"Failed to add contact to the bot's agenda using {contact}: {response.Reason.Description}", null, null, "");
                }
            }
            catch (AuthenticationException authex)
            {
                return StatusCode((int)HttpStatusCode.Unauthorized, authex);
            }
            catch (ApiException apiex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, apiex);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Updates a <c>Contact</c> on a bot's contact list
        /// </summary>
        /// <param name="authorization">The Authorization (Key) for the requested bot</param>
        /// <param name="contact">The contact to update on the bot's agenda</param>
        /// <param name="identity">The identity of the contact to be updated</param>
        /// <param name="querySample">Just a sample query param to show on swagger</param>
        /// <remarks>
        /// Sample request:
        ///     
        ///     PUT /contacts/11121023102013021@messenger.gw.msging.net HTTP/1.1
        ///     Host: localhost:XXXX
        ///     Authorization: Key YmFneTpzMDVUZGlvNmZLV2t5TDl6Njl4Wg=
        /// 
        ///     {  
        ///         "identity": "11121023102013021@messenger.gw.msging.net",
        ///         "name": "John Doe",
        ///         "gender":"male",
        ///         "group":"friends",    
        ///         "extras": {
        ///             "plan":"Gold",
        ///             "code":"1111"      
        ///         }
        ///     }
        /// </remarks>
        /// <response code="200">Successfully updated the <paramref name="contact"/></response>
        /// <response code="400">Mismatching <paramref name="identity"/> and <paramref name="contact"/> identity field</response>
        /// <response code="401">Missing or incomplete <paramref name="authorization"/> header</response>
        /// <response code="500">Not mapped exception. See <c>Exception</c> object thrown for details.</response>
        [HttpPut("{identity}")]
        [ProducesResponseType(typeof(Command), 200)]
        public async Task<IActionResult> PutAsync([FromHeader(Name = "Authorization"), Required] string authorization, [FromBody, Required] Contact contact, string identity, [FromQuery] string querySample)
        {
            try
            {
                CheckAuthorization(authorization);
                if (contact.Identity == null)
                {
                    throw new ArgumentException($"Body must be a Contact json containing the contact's Identity", nameof(contact));
                }

                if (!contact.Identity.ToString().Equals(identity))
                {
                    throw new ArgumentException($"Given identity does not match the contact's identity", nameof(identity));
                }

                var client = _factory.BuildBlipHttpClient(authorization);
                var command = new Command
                {
                    Method = CommandMethod.Merge,
                    Uri = new LimeUri($"/contacts"),
                    Resource = contact
                };
                var response = await client.ProcessCommandAsync(command, CancellationToken.None);

                if (response.Status == CommandStatus.Success)
                {
                    return StatusCode((int)HttpStatusCode.OK, response);
                }
                else
                {
                    throw new ApiException(HttpMethod.Get, new Uri("/contacts"), HttpStatusCode.BadRequest,
                        $"Failed to update contact on the bot's agenda using {contact}: {response.Reason.Description}",
                        null, null, "");
                }
            }
            catch (ArgumentException argex)
            {
                return StatusCode((int)HttpStatusCode.BadRequest, argex);
            }
            catch (AuthenticationException authex)
            {
                return StatusCode((int)HttpStatusCode.Unauthorized, authex);
            }
            catch (ApiException apiex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, apiex);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Deletes a given <c>Contact</c> from a bot's contact list
        /// </summary>
        /// <param name="authorization">The Authorization (Key) for the requested bot</param>
        /// <param name="identity">The desired contact's <c>identity</c></param>
        /// <remarks>
        /// Sample request:
        ///     
        ///     DELETE /contacts/useridentity.chatbot@0mn.io HTTP/1.1
        ///     Host: localhost:XXXX
        ///     Authorization: Key YmFneTpzMDVUZGlvNmZLV2t5TDl6Njl4Wg=
        ///     
        /// </remarks>
        /// <response code="200">Successfully deleted the <paramref name="identity"/>'s <c>Contact</c></response>
        /// <response code="401">Missing or incomplete <paramref name="authorization"/> header</response>
        /// <response code="500">Not mapped exception. See <c>Exception</c> object thrown for details.</response>
        [HttpDelete("{identity}")]
        [ProducesResponseType(typeof(Command), 200)]
        public async Task<IActionResult> DeleteAsync([FromHeader(Name = "Authorization"), Required] string authorization, string identity)
        {
            try
            {
                CheckAuthorization(authorization);

                var client = _factory.BuildBlipHttpClient(authorization);
                var command = new Command
                {
                    Method = CommandMethod.Delete,
                    Uri = new LimeUri($"/contacts/{identity}")
                };
                var response = await client.ProcessCommandAsync(command, CancellationToken.None);

                if (response.Status == CommandStatus.Success)
                {
                    return StatusCode((int)HttpStatusCode.OK, response);
                }
                else
                {
                    throw new ApiException(HttpMethod.Get, new Uri("/contacts/{identity}"), HttpStatusCode.BadRequest,
                        $"Failed to delete contact from bot's agenda using {identity}: {response.Reason.Description}", null, null, "");
                }
            }
            catch (AuthenticationException authex)
            {
                return StatusCode((int)HttpStatusCode.Unauthorized, authex);
            }
            catch (ApiException apiex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, apiex);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex);
            }
        }

        private static void CheckAuthorization(string authorization)
        {
            if (authorization == null || !authorization.StartsWith("Key "))
            {
                throw new AuthenticationException($"Missing or incomplete Authorization header. Given Authorization: {authorization}");
            }
        }
    }
}
