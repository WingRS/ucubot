using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Rewrite.Internal.PatternSegments;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using ucubot.Model;

namespace ucubot.Controllers
{
    [Route("api/[controller]")]
    public class LessonSignalEndpointController : Controller
    {
        private readonly IConfiguration _configuration;
        private string connectionString;
        public LessonSignalEndpointController(IConfiguration configuration)
        {
            _configuration = configuration;
            connectionString = _configuration.GetConnectionString("BotDatabase");

        }

        [HttpGet]
        public IEnumerable<LessonSignalDto> ShowSignals()
        {      
            IEnumerable<LessonSignalDto> lessonSignalDtos = new List<LessonSignalDto>();
            
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("select * from lesson_signal;",
                    connection))
                {

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {

                        while (reader.Read())
                        {
                            LessonSignalDto lessonSignalDto = new LessonSignalDto();
                            lessonSignalDto.Id = reader.GetInt32("id");
                            lessonSignalDto.Timestamp = reader.GetDateTime("timestamp_");
                            lessonSignalDto.UserId = reader.GetString("user_id");
                            lessonSignalDto.Type =
                                SignalTypeUtils.ConvertSlackMessageToSignalType(reader.GetString("signal_type"));
                            lessonSignalDtos.Append(lessonSignalDto);
                        }

                    }
                }
            }

            return lessonSignalDtos;
            
        }

        [HttpGet("{id}")]
        public LessonSignalDto ShowSignal(long id)
        {
            
            LessonSignalDto lessonSignalDto = new LessonSignalDto();
            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("select * from lesson_signal where id='@id';",
                    connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            throw new System.Exception();
                        }
                        while (reader.Read())
                        {
                            lessonSignalDto.Id = reader.GetInt32("id");
                            lessonSignalDto.Timestamp = reader.GetDateTime("timestamp_");
                            lessonSignalDto.UserId = reader.GetString("user_id");
                            lessonSignalDto.Type =
                                SignalTypeUtils.ConvertSlackMessageToSignalType(reader.GetString("signal_type"));

                        }

                    }
                }

            



            return lessonSignalDto;

        }

        [HttpPost]
        public async Task<IActionResult> CreateSignal(SlackMessage message)
        {
            var userId = message.user_id;
            LessonSignalType signalType;
            try
            {
                signalType = message.text.ConvertSlackMessageToSignalType();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return Forbid();
            }
            


            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                String str =
                    "INSERT INTO lesson_signal (signal_type, timestamp_, userId) VALUES (@signalType, @Date, @userId)";
                
                using (MySqlCommand cmd = new MySqlCommand(str, connection))
                {
                    
                    cmd.Parameters.AddWithValue("@signalType", signalType);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@Date", DateTime.Now);
                    if (cmd.ExecuteNonQuery() == -1)
                    {
                        return Forbid();
                    }
                }
                connection.Close();
            }



            return Accepted();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveSignal(long id)
        {
            if (id <= 0)
            {
                return Forbid();
            }            


          

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("delete * from lesson_signal where id='@id';",
                    connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQueryAsync();
                }
                connection.Close();

            }

            return Accepted();
        }
    }
}

