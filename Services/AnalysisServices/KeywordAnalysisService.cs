using glint_backend.Models;
using glint_backend.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace glint_backend.Services.AnalysisServices;

/// <summary>
/// Domain-agnostic keyword analysis.
///
/// Key change from v1: replaced the hardcoded <c>KnownTechTerms</c> list and
/// <c>IsTechnicalTerm</c> heuristics with a vocabulary-rarity approach.
///
/// How importance is determined:
///   A token is "important" when it is rare in everyday English — i.e. not in
///   <see cref="CommonEnglishVocab"/> — OR when the job ad itself repeats it
///   two or more times (repetition = emphasis, regardless of domain).
///
/// This means "React", "triage", "FHIR", "derivatives", and "conveyancing" all
/// score as important without any domain-specific list, while "work", "team",
/// "develop", and "experience" are treated as generic noise and weighted 1×.
///
/// Scoring is otherwise identical to v1:
///   • Important tokens   → 2× weight
///   • General tokens     → 1× weight
///   • Bigrams blended in at 20 %
/// </summary>
public class KeywordAnalysisService : IKeywordAnalysisService
{
    // ─────────────────────────────────────────────────────────────────────────
    // Section detection  (unchanged from v1)
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly string[] RequirementsMarkers =
    [
        "requirements",
        "qualifications",
        "we are looking for",
        "what we're looking for",
        "looking for someone",
        "your background",
        "you have",
        "you bring",
        "meritorious",
        "must have",
        "minimum qualifications",
        "skills needed",
        "your tasks include",
    ];

    private static readonly string[] NoiseMarkers =
    [
        "about us",
        "about the company",
        "about karolinska",
        "about the hospital",
        "about the recruitment",
        "recruitment process",
        "what we offer",
        "we offer",
        "you are offered",
        "benefits",
        "equal opportunity",
        "diversity",
        "applying for a job",
        "please read more",
        "follow us on",
        "before filling",
        "as an employee",
    ];

    // ─────────────────────────────────────────────────────────────────────────
    // Stop words  (function words — never meaningful)
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an",
        "and", "but", "or", "nor", "so", "yet", "for", "both", "either", "neither",
        "in", "on", "at", "to", "of", "by", "as", "up", "off",
        "from", "into", "onto", "over", "under", "with", "without",
        "about", "above", "below", "between", "among", "through",
        "during", "before", "after", "since", "until", "via", "per",
        "i", "we", "you", "he", "she", "it", "they", "them", "their",
        "our", "your", "his", "her", "its", "who", "what", "which",
        "this", "that", "these", "those",
        "is", "are", "was", "were", "be", "been", "being",
        "has", "have", "had", "do", "does", "did",
        "will", "would", "shall", "should", "can", "could", "may", "might", "must",
        "also", "etc", "such", "than", "then", "when", "where",
        "all", "any", "each", "very", "more", "most", "some",
        "not", "no", "nor", "if", "else", "how", "just", "only",
        "well", "get", "got", "let", "put", "set",
    };

    // ─────────────────────────────────────────────────────────────────────────
    // Common English vocabulary
    //
    // Words here are "too common to carry signal" across all domains.
    // Anything NOT in this set (and not a stop word) is treated as important
    // and weighted 2× — no domain knowledge required.
    //
    // Covers:
    //   • High-frequency action verbs  (work, develop, manage …)
    //   • Generic nouns                (team, role, person, year …)
    //   • Job-ad boilerplate           (apply, candidate, position …)
    //   • Common adjectives/adverbs    (good, new, strong, further …)
    //   • Numbers / short tokens filtered elsewhere
    //
    // NOT included: domain terms (React, triage, FHIR, derivatives …)
    // Even if a domain term somehow crept in, it would only lose its 2× weight,
    // not disappear from the match — the downside is small.
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly HashSet<string> CommonEnglishVocab = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── Action verbs ────────────────────────────────────────────────────
        "work", "works", "working", "worked",
        "develop", "develops", "developing", "developed", "development",
        "build", "builds", "building", "built",
        "create", "creates", "creating", "created",
        "make", "makes", "making", "made",
        "manage", "manages", "managing", "managed", "management",
        "lead", "leads", "leading", "led",
        "drive", "drives", "driving", "driven",
        "support", "supports", "supporting", "supported",
        "help", "helps", "helping", "helped",
        "use", "uses", "using", "used",
        "apply", "applies", "applying", "applied", "application",
        "provide", "provides", "providing", "provided",
        "ensure", "ensures", "ensuring", "ensured",
        "improve", "improves", "improving", "improved", "improvement",
        "maintain", "maintains", "maintaining", "maintained",
        "implement", "implements", "implementing", "implemented",
        "design", "designs", "designing", "designed",
        "deliver", "delivers", "delivering", "delivered", "delivery",
        "solve", "solves", "solving", "solved",
        "define", "defines", "defining", "defined",
        "identify", "identifies", "identifying", "identified",
        "plan", "plans", "planning", "planned",
        "analyze", "analyzes", "analyzing", "analyzed", "analyse", "analyses",
        "review", "reviews", "reviewing", "reviewed",
        "test", "tests", "testing", "tested",
        "deploy", "deploys", "deploying", "deployed",
        "run", "runs", "running",
        "write", "writes", "writing", "written",
        "read", "reads", "reading",
        "learn", "learns", "learning", "learned",
        "grow", "grows", "growing", "grown",
        "share", "shares", "sharing", "shared",
        "meet", "meets", "meeting", "met",
        "follow", "follows", "following", "followed",
        "include", "includes", "including", "included",
        "continue", "continues", "continuing", "continued",
        "understand", "understands", "understanding", "understood",
        "focus", "focuses", "focusing", "focused",
        "contribute", "contributes", "contributing", "contributed", "contribution",
        "communicate", "communicates", "communicating", "communicated", "communication",
        "collaborate", "collaborates", "collaborating", "collaborated", "collaboration",
        "coordinate", "coordinates", "coordinating", "coordinated",
        "optimize", "optimizes", "optimizing", "optimized",
        "monitor", "monitors", "monitoring", "monitored",
        "report", "reports", "reporting", "reported",
        "handle", "handles", "handling", "handled",
        "take", "takes", "taking", "taken",
        "give", "gives", "giving", "given",
        "find", "finds", "finding", "found",
        "need", "needs", "needing", "needed",
        "want", "wants", "wanting", "wanted",
        "know", "knows", "knowing", "known",
        "think", "thinks", "thinking", "thought",
        "bring", "brings", "bringing", "brought",
        "come", "comes", "coming",
        "go", "goes", "going", "went", "gone",
        "see", "sees", "seeing", "seen", "saw",
        "look", "looks", "looking", "looked",
        "show", "shows", "showing", "showed", "shown",
        "keep", "keeps", "keeping", "kept",
        "hold", "holds", "holding", "held",
        "move", "moves", "moving", "moved",
        "change", "changes", "changing", "changed",
        "increase", "increases", "increasing", "increased",
        "reduce", "reduces", "reducing", "reduced",
        "allow", "allows", "allowing", "allowed",
        "require", "requires", "requiring", "required",
        "expect", "expects", "expecting", "expected",
        "consider", "considers", "considering", "considered",
        "achieve", "achieves", "achieving", "achieved",
        "drive", "enable", "facilitate",
        "connect", "connecting", "connected",
        "integrate", "integrating", "integrated",

        "someone", "anyone", "everyone", "nobody",
        "example", "examples", "instance", "instances",
        "assistant", "assistants",
        "code", "coding", "script",
        "database", "databases",
        "task", "tasks",

        // ── Common nouns ────────────────────────────────────────────────────
        "team", "teams",
        "role", "roles",
        "person", "people",
        "year", "years",
        "time", "times",
        "day", "days",
        "month", "months",
        "week", "weeks",
        "part", "parts",
        "area", "areas",
        "field", "fields",
        "level", "levels",
        "type", "types",
        "way", "ways",
        "case", "cases",
        "point", "points",
        "place", "places",
        "line", "lines",
        "step", "steps",
        "task", "tasks",
        "goal", "goals",
        "result", "results",
        "outcome", "outcomes",
        "impact", "impacts",
        "value", "values",
        "problem", "problems",
        "solution", "solutions",
        "question", "questions",
        "issue", "issues",
        "change", "changes",
        "process", "processes",
        "project", "projects",
        "product", "products",
        "service", "services",
        "system", "systems",
        "tool", "tools",
        "resource", "resources",
        "information", "data",
        "knowledge", "skill", "skills",
        "ability", "abilities",
        "experience", "experiences",
        "background", "backgrounds",
        "interest", "interests",
        "passion",
        "responsibility", "responsibilities",
        "opportunity", "opportunities",
        "challenge", "challenges",
        "benefit", "benefits",
        "requirement", "requirements",
        "qualification", "qualifications",
        "position", "positions",
        "job", "jobs",
        "career",
        "organization", "organisations", "organization", "organizations",
        "company", "companies",
        "department", "departments",
        "division",
        "office",
        "environment", "environments",
        "culture",
        "mission",
        "vision",
        "strategy", "strategies",
        "approach", "approaches",
        "method", "methods",
        "model", "models",
        "framework", "frameworks",
        "standard", "standards",
        "practice", "practices",
        "principle", "principles",
        "policy", "policies",
        "program", "programs", "programme", "programmes",
        "initiative", "initiatives",
        "effort", "efforts",
        "meeting", "meetings",
        "discussion", "discussions",
        "decision", "decisions",
        "plan", "plans",
        "idea", "ideas",
        "concept", "concepts",
        "context",
        "purpose",
        "scope",
        "range",
        "focus",
        "basis",
        "end",
        "start",
        "set",
        "group", "groups",
        "member", "members",
        "partner", "partners",
        "client", "clients",
        "customer", "customers",
        "user", "users",
        "stakeholder", "stakeholders",
        "candidate", "candidates",
        "applicant", "applicants",
        "employee", "employees",
        "colleague", "colleagues",
        "manager", "managers",
        "leader", "leaders",
        "specialist", "specialists",
        "expert", "experts",
        "developer", "developers",
        "engineer", "engineers",
        "analyst", "analysts",
        "consultant", "consultants",

        // ── Job-ad boilerplate ───────────────────────────────────────────────
        "apply", "applying", "application",
        "hire", "hiring",
        "recruit", "recruiting", "recruitment",
        "interview",
        "deadline",
        "cv", "resume",
        "permanent", "temporary", "full-time", "fulltime", "part-time", "parttime",
        "remote", "hybrid", "onsite", "office",
        "salary", "compensation", "package",
        "welcome",
        "opportunity",
        "offer",
        "join",

        // ── Common adjectives / adverbs ─────────────────────────────────────
        "good", "great", "excellent", "outstanding",
        "strong", "solid", "robust",
        "new", "current", "existing", "latest",
        "key", "core", "main", "primary", "secondary",
        "general", "specific", "particular",
        "large", "small", "big",
        "high", "low",
        "long", "short",
        "fast", "quick",
        "clear", "simple", "easy",
        "complex", "advanced",
        "modern", "standard",
        "important", "critical", "essential", "necessary",
        "relevant", "related",
        "broad", "wide",
        "multiple", "various", "different", "diverse",
        "cross", "functional",
        "independent", "structured",
        "professional", "technical",
        "further", "forward",
        "together",
        "close", "closely",
        "direct", "directly",
        "active", "actively",
        "effective", "effectively",
        "efficient", "efficiently",
        "responsible",
        "motivated",
        "driven",
        "passionate",
        "dedicated",
        "detail",
        "oriented",
        "based",
        "focused",
        "related",
        "following",
        "including",
        "etc",
        "ie",
        "eg",
        "next",
        "other",
        "both",
        "many",
        "several",
        "few",
        "every",
        "whole",
        "full",
        "right",
        "own",
        "same",
        "similar",
        "possible",
        "able",
        "available",
        "free",
        "open",
        "ready",

        // ── Common adverbs / connectives ─────────────────────────────────────
        "however", "therefore", "although", "because", "while",
        "together", "further", "within", "across", "around",
        "already", "often", "always", "sometimes", "usually",
        "mainly", "primarily", "especially", "particularly",
        "including", "alongside", "additionally", "furthermore",
        "ideally", "preferably", "typically",

        // ── job-ad qualifier boilerplate (new) ───────────────────────────────
        "documented",           // "documented experience in X" appears 5–6× in every ad
        "meritorious",          // Swedish job-ad term for "nice to have"
        "merit",
        "merits",
        "structured",           // "works in a structured way"
        "structurally",
        "independently",
        "independent",
        "manner",               // "in an independent manner"
        "sustainable",          // "finds sustainable solutions"
        "conscious",            // "quality-conscious"
        "carefully",
        "prerequisite",
        "prerequisite",
        "continuously",
        "continuously",
        "complete",
        "briefly",
        "explain",
        "suitable",

        // ── org/geography noise (new) ────────────────────────────────────────
        "sector",               // "public sector" — relevant phrase but "sector" alone is noise
        "public",
        "national",
        "regional",
        "region",               // "Stockholm region" — too generic on its own
        "municipal",
        "municipality",
        "hospital",             // alone it's noise; "university hospital" isn't
        "register",             // "quality registers"
        "registers",
        "record",               // "medical record"
        "records",
        "component",
        "components",
        "object",
        "objects",
        "party",
        "parties",
        "contractual",
        "provider",
        "providers",
        "supplier",
        "suppliers",
        "representative",
        "representatives",
        "specialist",           // already in list but double-check
        "flow",
        "flows",
        "interface",            // alone it's generic — "integrations and interfaces"
        "interfaces",           // the bigram matters more than either word alone
        "generation",
        "next",                 // "next generation"
        "waiting",
        "transfer",
        "transferring",
        "transferable",
        "attach",               // "attach a CV"
        "attachment",

        // ── common adjectives missed from original list (new) ────────────────
        "socially",
        "social",
        "complex",
        "developing",           // "developing environment" — too vague
        "current",
        "currently",
        "general",
        "ordinary",
        "ordinary",
        "special",
        "specially",
        "specialized",
        "important",            // "important role" is noise
        "closely",
        "close",
        "central",
        "cross",
        "functional",           // "cross-functional" — keep the compound, not the parts
        "balance",              // "work-life balance"
        "life",
        "mission",
        "vision",
        "proud",
        "leading",
        "world",
        "attractive",
        "equal",
        "equality",
        "equity",
        "anonymous",
        "anonymous",
        "protected",
        "sensitive",
        "careful",
        "relevant",
        "complete",
        "complete",
        "continuously",
        "selection",
        "interview",
        "interviews",
        "probationary",
        "period",
        "criminal",
        "war",
        "deployed",

        // ── temporal/quantifier noise (new) ──────────────────────────────────
        "five",
        "least",                // "at least five years"
        "years",                // already in list — confirm
        "days",
        "clock",
        "tomorrow",
        "today",
        "deadline",

        // ── job-ad qualifier boilerplate ────────────────────────────────────────────
        "documented",       // "documented experience in X" ×6
        "meritorious",      // Swedish nice-to-have marker
        "structured",       // "works in a structured way"
        "sustainable",      // "finds sustainable solutions"
        "conscious",        // "quality-conscious"
        "manner",           // "in an independent manner"
        "prerequisite",
        "continuously",
        "complete",
        "briefly",
        "suitable",
        "prerequisite",

        // ── org/geography noise ──────────────────────────────────────────────────────
        "sector",           // "public sector" — the compound is relevant, not the word
        "public",
        "national",
        "regional",
        "hospital",
        "register",
        "registers",
        "record",
        "records",
        "component",
        "components",
        "party",
        "provider",
        "providers",
        "supplier",
        "suppliers",
        "representative",
        "representatives",
        "flow",
        "flows",
        "interface",        // bigram "integrations interfaces" is the signal
        "interfaces",
        "generation",

        // ── soft skill filler ────────────────────────────────────────────────────────
        "structured",
        "independently",
        "manner",
        "sustainable",
        "conscious",
        "carefully",
        "proactively",
        "motivated",
        "social",
        "socially",
        "complex",
        "central",
        "cross",
        "functional",       // keep "cross-functional" as a bigram but not the parts
        "balance",
        "life",
        "proud",
        "attractive",
        "equal",
        "equality",
        "equity",
        "anonymous",
        "protected",
        "probationary",
        "criminal",
        "deployed",
        "war",

        // ── quantifiers that appear in requirements but carry no CV signal ───────────
        "five",
        "least",
        "modern",           // "modern web frameworks" — "modern" alone is noise
        "efficiency",       // debatable, but appears in boilerplate context here

        // Generic tech-job adjectives/nouns that are NOT domain signal
        "technical",        // "technical solutions", "technical problems" — too vague
        "business",         // "business representatives" — noise
        "methods",          // "working methods" — noise  
        "flows",            // "information flows" — noise
        "deliveries",       // "technical deliveries" — noise
        "Qualifications",   // section header
        "quality",          // "data quality", "quality-conscious" — too generic alone
        "conscious",        // "quality-conscious"
        "optimizing",       // "optimizing SQL queries" — the SQL is the signal, not "optimizing"
        "optimization",
        "interfaces",       // "integrations and interfaces" — interfaces alone is noise; bigram carries the signal
        "queries",          // alone it's generic; "SQL queries" bigram is the signal
        "sector",
        "public",
        "national",
        "register",
        "registers",
        "record",
        "records",
        "structured",
        "sustainable",
        "manner",
        "documented",
        "meritorious",
        "prerequisite",
        "efficiency",
        "continuously",
        "suitable",

        // These cover cross-line bigram leakage even when vocab check fails
        "further",          // "further developing" — "further" alone is noise
        "natural",          // "natural part of the role"
        "forward",          // "drives your work forward"
        "carefully",
        "analyzes",
        "analyze",
        "interest",
        "interests",
        "independently",
    };

    // ─────────────────────────────────────────────────────────────────────────
    // Separators  (unchanged from v1)
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly char[] Separators =
    [
        ' ', '\n', '\r', '\t', ',', '.', ';', ':', '(', ')', '[', ']', '{', '}', '/', '\\', '|', '<', '>', '-', '•'
    ];

    // ─────────────────────────────────────────────────────────────────────────
    // Importance scoring  (replaces IsTechnicalTerm)
    //
    // A token is "important" when either:
    //   a) It appears ≥ 2 times in the job ad (repetition = deliberate emphasis)
    //   b) It is absent from CommonEnglishVocab (rare in everyday English)
    //
    // Neither condition requires any domain knowledge.
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly HashSet<string> KnownAcronyms = new(StringComparer.Ordinal)
    {
        "C#", "SQL", "API", "AI", ".NET", "IT", "CI", "CD", "UI", "UX",
        "ORM", "JWT", "HTTP", "REST", "XML", "JSON", "CSS", "HTML",
    };

    private static bool IsKnownAcronym(string token) =>
        KnownAcronyms.Contains(token) || token.All(c => char.IsUpper(c) || c == '#' || c == '.');

    private bool IsImportantToken(
    string token,
    IReadOnlyDictionary<string, int> jobAdFrequency)
    {
        // 1. Known acronyms (.NET, API, AI) are always important
        if (KnownAcronyms.Contains(token))
            return true;

        // 2. Reject short words that aren't acronyms (keeps out noise)
        if (token.Length < 4)
            return false;

        // 3. If it's a common everyday word ("work", "team", "develop"), treat as generic noise (1x weight)
        if (CommonEnglishVocab.Contains(token))
        {
            // OPTIONAL: If you want heavily repeated generic words to become "important", uncomment the next line:
            // if (jobAdFrequency.TryGetValue(token, out int freq) && freq >= 3) return true;

            return false;
        }

        // 4. If it's NOT in the common vocabulary (e.g. "React", "SQL", "conveyancing"), it's an important domain keyword!
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public interface
    // ─────────────────────────────────────────────────────────────────────────

    public Task<(decimal Score, string Feedback)> AnalyzeAsync(
        PdfDocumentData pdfData,
        JobAdvertisement jobAdvertisement)
    {
        var resumeText = pdfData.Text ?? string.Empty;
        var requirementsText = ExtractRequirementsSection(jobAdvertisement.RawText);

        // --- Token sets ---
        var jobTokens = ExtractTokens(requirementsText);
        var cvTokens = ExtractTokens(resumeText);

        // Build a frequency map for the requirements section so that
        // repeated terms get the importance boost.
        var jobAdFrequency = BuildFrequencyMap(requirementsText);

        // --- Bigrams ---
        var jobBigrams = ExtractBigrams(requirementsText, jobAdFrequency);
        var cvBigrams = ExtractBigrams(resumeText, jobAdFrequency);

        // --- Split tokens by importance ---
        var jobImportantTokens = jobTokens
            .Where(t => IsImportantToken(t, jobAdFrequency))
            .ToList();

        var jobGeneralTokens = jobTokens
            .Except(jobImportantTokens, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // --- Match / miss ---
        var matchedImportant = jobImportantTokens
            .Intersect(cvTokens, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var matchedGeneral = jobGeneralTokens
            .Intersect(cvTokens, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var missingImportant = jobImportantTokens
            .Except(cvTokens, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var missingGeneral = jobGeneralTokens
            .Except(cvTokens, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var matchedBigrams = jobBigrams
            .Intersect(cvBigrams, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var missingBigrams = jobBigrams
            .Except(cvBigrams, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // --- Score ---
        // Important tokens 2×, general tokens 1× (same weighting as v1 tech/general split)
        var totalTokenPts = (jobImportantTokens.Count * 2) + jobGeneralTokens.Count;
        var matchedTokenPts = (matchedImportant.Count * 2) + matchedGeneral.Count;

        var tokenScore = totalTokenPts == 0
            ? 100m
            : Math.Round((decimal)matchedTokenPts / totalTokenPts * 100, 1);

        var bigramScore = jobBigrams.Count == 0
            ? 100m
            : Math.Round((decimal)matchedBigrams.Count / jobBigrams.Count * 100, 1);

        var score = Math.Clamp(
            Math.Round(tokenScore * 0.90m + bigramScore * 0.10m, 1),
            0m, 100m);

        // --- Feedback payload ---
        // Surface important tokens first (most signal), then bigrams, then general.
        var topMatched = matchedImportant
            .Concat(matchedBigrams)
            .Concat(matchedGeneral)
            .Distinct()
            //.Take(25) // For now, include all matches — the UI can decide how many to show
            .ToList();

        var topMissing = missingImportant
            .Concat(missingBigrams)
            .Concat(missingGeneral)
            .Distinct()
            //.Take(25) // For now, include all misses — the UI can decide how many to show
            .ToList();

        string tip;
        if (topMissing.Count == 0)
        {
            tip = "Excellent coverage! Your resume closely mirrors the core technical terminology and phrases found in the job ad.";
        }
        else if (missingImportant.Count > 0)
        {
            tip = "Tip: Consider adding some of these missing key terms to your skills or summary section. (Note: Our system extracts terms based on rarity, so use your best judgment to only include terms that genuinely match your experience!)";
        }
        else
        {
            tip = "Tip: You've hit the main keywords, but try weaving some of these specific phrases into your role descriptions for even better alignment.";
        }

        var feedback = JsonSerializer.Serialize(new
        {
            summary = $"Key terms: {matchedImportant.Count}/{jobImportantTokens.Count} · Keywords: {matchedGeneral.Count}/{jobGeneralTokens.Count} · Phrases: {matchedBigrams.Count}/{jobBigrams.Count}",
            matched = topMatched,
            missing = topMissing,
            tip,
        });

        return Task.FromResult((score, feedback));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Requirements section extraction  (unchanged from v1)
    // ─────────────────────────────────────────────────────────────────────────

    private static string ExtractRequirementsSection(string jobAdText)
    {
        if (string.IsNullOrWhiteSpace(jobAdText))
            return jobAdText;

        var lines = jobAdText
            .Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .ToList();

        var start = 0;
        for (var i = 0; i < lines.Count; i++)
        {
            var lower = lines[i].ToLowerInvariant();
            if (RequirementsMarkers.Any(m => lower.Contains(m)))
            {
                start = i;
                break;
            }
        }

        var end = lines.Count;
        for (var i = start + 1; i < lines.Count; i++)
        {
            var lower = lines[i].ToLowerInvariant();
            if (NoiseMarkers.Any(m => lower.Contains(m)))
            {
                end = i;
                break;
            }
        }

        var section = string.Join("\n", lines.Skip(start).Take(end - start));
        return section.Length >= 80 ? section : jobAdText;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Frequency map
    // ─────────────────────────────────────────────────────────────────────────

    private Dictionary<string, int> BuildFrequencyMap(string text)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in Tokenize(text))
        {
            map[token] = map.TryGetValue(token, out var n) ? n + 1 : 1;
        }
        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Token extraction
    // ─────────────────────────────────────────────────────────────────────────

    private HashSet<string> ExtractTokens(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return Tokenize(text).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Bigram extraction
    //
    // A bigram is kept when at least one token is important (rare in English
    // or repeated in this ad), or both tokens are long (> 5 chars).
    // The jobAdFrequency map is passed in so IsImportantToken can be called.
    // ─────────────────────────────────────────────────────────────────────────

    private List<string> ExtractBigrams(
    string text,
    IReadOnlyDictionary<string, int> jobAdFrequency)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        // Split each line individually — bullet points must NEVER
        // contribute tokens to bigrams across their boundary.
        // Also split on ':' so "Tasks include: developing" doesn't
        // produce the bigram "include developing".
        var sentences = text.Split(
            ['.', '\n', '\r', '!', '?', ';', ':'],
            StringSplitOptions.RemoveEmptyEntries);

        var bigrams = new List<string>();
        foreach (var sentence in sentences)
        {
            var tokens = Tokenize(sentence).ToList();

            // Require BOTH tokens to be important and BOTH to be
            // non-trivially long. This cuts "structured way",
            // "independent manner", "sustainable solutions" etc.
            for (var i = 0; i < tokens.Count - 1; i++)
            {
                var t1 = tokens[i];
                var t2 = tokens[i + 1];

                if (t1.Length > 2 && t2.Length > 2
                    && IsImportantToken(t1, jobAdFrequency)
                    && IsImportantToken(t2, jobAdFrequency))
                {
                    bigrams.Add($"{t1} {t2}");
                }
            }
        }

        return bigrams.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tokenizer
    // FIX retained from v1: TrimEnd only — preserves leading dot on ".NET"
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerable<string> Tokenize(string text) =>
        text
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.TrimEnd('.').TrimStart('_', '*', '+'))
            .Where(t =>
                t.Length > 1 &&
                t.Any(char.IsLetter) &&
                !t.All(char.IsDigit) &&
                !StopWords.Contains(t));

    // DEBUGGING INTERFACE: lets you see exactly what the service is extracting from a job ad before any CV comparison happens.
    public Task<KeywordDebugInfo> DebugExtractAsync(string rawJobAdText)
    {
        var requirementsText = ExtractRequirementsSection(rawJobAdText);
        var jobAdFrequency = BuildFrequencyMap(requirementsText);
        var jobTokens = ExtractTokens(requirementsText);
        var jobBigrams = ExtractBigrams(requirementsText, jobAdFrequency);

        var importantTokens = jobTokens
            .Where(t => IsImportantToken(t, jobAdFrequency))
            .OrderByDescending(t => jobAdFrequency.GetValueOrDefault(t))
            .Select(t =>
            {
                var freq = jobAdFrequency.GetValueOrDefault(t);
                // Show WHY each token was classified as important
                string reason;
                if (KnownAcronyms.Contains(t))
                    reason = "known acronym";
                else if (t.Length < 4)
                    reason = "short — should have been filtered";
                else if (CommonEnglishVocab.Contains(t))
                    reason = "BUG: in vocab but still important";
                else
                    reason = $"not in common vocab (freq={freq})";
                return new DebugToken(t, freq, reason);
            })
            .ToList();

        var generalTokens = jobTokens
            .Except(importantTokens.Select(x => x.Token), StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t)
            .ToList();

        // NEW: show section extraction diagnostics
        var sectionStartLine = rawJobAdText
            .Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Select((line, idx) => (line, idx))
            .FirstOrDefault(x => RequirementsMarkers.Any(m =>
                x.line.ToLowerInvariant().Contains(m)));

        var result = new KeywordDebugInfo
        {
            ScoredText = requirementsText,
            ScoredTextLengthVsTotal = $"{requirementsText.Length}/{rawJobAdText.Length} chars " +
                $"({100 * requirementsText.Length / rawJobAdText.Length}% of ad)",
            SectionStartedAt = sectionStartLine.line ?? "FULL TEXT USED — no marker found",
            ImportantTokens = importantTokens,
            GeneralTokens = generalTokens,
            Bigrams = jobBigrams,
            TokenFrequency = jobAdFrequency,
        };

        return Task.FromResult(result);
    }
}