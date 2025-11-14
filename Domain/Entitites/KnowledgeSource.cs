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

        [Required, MaxLength(50)]
        public string Type { get; set; }

        public string Source { get; set; }

        public string MetaJson { get; set; }

        /// <summary>
        /// Original title or file name for display purposes
        /// </summary>
        [MaxLength(255)]
        public string Title { get; set; }

        /// <summary>
        /// Stored text content (chunk) extracted from the source
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 0-based chunk index for pagination/ordering
        /// </summary>
        public int? ChunkIndex { get; set; }

        /// <summary>
        /// Total number of chunks created for the same upload
        /// </summary>
        public int? TotalChunks { get; set; }

        /// <summary>
        /// 1-based page number (ChunkIndex + 1), for UI-friendly pagination
        /// </summary>
        public int? PageNumber { get; set; }

        // Which AI_Configure this belongs to
        public Guid AIConfigureId { get; set; }
        public virtual AI_Configure AI_Configure { get; set; }
    }
}
