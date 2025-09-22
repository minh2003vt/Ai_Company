using Domain.Entitites;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entitites
{
    public class KnowledgeSource
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        /// <summary>
        /// Type: "vector-db", "file", "url", "cloud-storage"
        /// </summary>
        [Required, MaxLength(50)]
        public string Type { get; set; }

        /// <summary>
        /// e.g. vector DB identifier (index name), file path, bucket url, or external id
        /// </summary>
        [Required, MaxLength(1000)]
        public string Source { get; set; }

        /// <summary>
        /// Optional metadata, e.g. JSON details like connection info (but keep secrets out of DB).
        /// </summary>
        public string MetaJson { get; set; }

        // Which AI_Configure this belongs to
        public Guid AIConfigureId { get; set; }
        public virtual AI_Configure AI_Configure { get; set; }
    }
}
