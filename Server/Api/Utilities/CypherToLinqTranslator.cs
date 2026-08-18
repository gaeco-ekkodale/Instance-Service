// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Lucene.Net.Analysis.Compound.Hyphenation;
using Microsoft.IdentityModel.Tokens;
using System.Reactive.Joins;
using System.Text.RegularExpressions;
using static InstanceService.Api.Utilities.CypherToLinqTranslator;

namespace InstanceService.Api.Utilities;

/// <summary>
/// Interface for translating a Cypher query into Linq.
/// </summary>
public interface ICypherToLinqTranslator
{
    /// <summary>
    /// Translates a Cypher query using the provided variable names.
    /// </summary>
    /// <param name="cypher">The Cypher query string.</param>
    /// <param name="variableNames">Object containing variable names for the relation.</param>
    /// <returns>The translation result.</returns>
    TranslationResult InterpreteCypher(string cypher, RelationVariableNames variableNames);
}

/// <summary>
/// Translator class that converts Cypher queries into Linq expressions.
/// This solution should be temporary due to being very brittle
/// Better Implementation with modular parts (Build real parser or use modular parts as api parameters)
/// </summary>
public class CypherToLinqTranslator() : ICypherToLinqTranslator
{
    /// <summary>
    /// The list of allowed command words in the Cypher query.
    /// </summary>
    private string[] _allowedCmds { get; set; } = ["MATCH", "WHERE", "RETURN"];

    /// <summary>
    /// Enum for allowed command words
    /// </summary>
    private enum Cmds
    {
        Match,
        Where,
        Return
    }

    /// <inheritdoc/>
    public TranslationResult InterpreteCypher(string cypher, RelationVariableNames variableNames)
    {
        cypher = RemoveComments(cypher);
        var userInput = ExtractUserInput(cypher);

        var selectStatement = ExtractCmd(cypher, _allowedCmds[(int)Cmds.Return]);
        var variablesInsideSelect = selectStatement
            .Split(",")
            .ToList()
            .Select(variable => variable = variable.Trim());

        var whereStatement = ExtractCmd(cypher, _allowedCmds[(int)Cmds.Where]);
        
        return new TranslationResult
        {
            LinqWhere = TrimSpaces(ReplaceFunctions(ReplaceOperators(ReplaceProperties(ReplaceVariableNames(AddBraceWhitespaces(ReplaceSymbols(whereStatement)), variableNames, userInput))))).Trim(),
            ReturnContainsSubject = variablesInsideSelect.Contains(userInput.Subject),
            ReturnContainsPredicate = variablesInsideSelect.Contains(userInput.Predicate),
            ReturnContainsObject = variablesInsideSelect.Contains(userInput.Object),
        };
    }

    /// <summary>
    /// Extract content of strings and returns the values inside.
    /// </summary>
    /// <param name="statement">The statement.</param>
    /// <returns>A list of string contents.</returns>
    private static List<string> ExtractStringContent(string statement)
    {
        var stringPattern = @"""(.*?)""";

        MatchCollection matches = Regex.Matches(statement, stringPattern);
        
        return matches.Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .ToList();
    }

    /// <summary>
    /// Removes all contents of strings.
    /// </summary>
    /// <param name="statement">The statement.</param>
    /// <returns>The manipulated statement.</returns>
    private static string RemoveStringContent(string statement)
    {
        return Regex.Replace(statement, @"""(.*?)""", "\"\"");
    }

    /// <summary>
    /// Replaces content of strings with the values of the parameter list.
    /// </summary>
    /// <param name="statement">The statement.</param>
    /// <param name="stringContents">The Contents of a string.</param>
    /// <returns>The statement filled with the given list of string contents.</returns>
    private static string AddStringContents(string statement, List<string> stringContents)
    {
        int matchIndex = 0;
        string pattern = @"""""";

        return Regex.Replace(statement, pattern, match =>
        {
            if (matchIndex < stringContents.Count)
            {
                string content = stringContents[matchIndex++];
                return $"\"{content}\"";
            }
            throw new ArgumentException("More values to inject than string literals.");
        });
    }

    /// <summary>
    /// Replaces variable names in the WHERE statement based on the mapping.
    /// </summary>
    /// <param name="statement">The statement to process.</param>
    /// <param name="variableNames">Mapped variable names object.</param>
    /// <param name="userInput">Extracted variable names from user input.</param>
    /// <returns>The statement with replaced variable names.</returns>
    private static string ReplaceVariableNames(string statement, RelationVariableNames variableNames, RelationVariableNames userInput)
    {
        var StringContents = ExtractStringContent(statement);
        var NoStringCypher = RemoveStringContent(statement);

        if (!userInput.Subject.IsNullOrEmpty())
            NoStringCypher = Regex.Replace(NoStringCypher, $@"(\s+|^){userInput.Subject}\.", $" {variableNames.Subject}.");
        if (!userInput.Predicate.IsNullOrEmpty())
            NoStringCypher = Regex.Replace(NoStringCypher, $@"(\s+|^){userInput.Predicate}\.", $" {variableNames.Predicate}.");
        if (!userInput.Object.IsNullOrEmpty())
            NoStringCypher = Regex.Replace(NoStringCypher, $@"(\s+|^){userInput.Object}\.", $" {variableNames.Object}.");

        return AddStringContents(NoStringCypher, StringContents);
    }

    /// <summary>
    /// Removes unnecessary white spaces from the Cypher statement.
    /// </summary>
    /// <param name="cypher">The Cypher statement.</param>
    /// <returns>The trimmed string.</returns>
    private static string TrimSpaces(string cypher)
    {
        var StringContents = ExtractStringContent(cypher);
        var NoStringCypher = RemoveStringContent(cypher);

        var result = Regex.Replace(NoStringCypher, @"\s+", " ");

        return AddStringContents(NoStringCypher, StringContents);
    }

    /// <summary>
    /// Adds whitespaces to braces for ReplaceVariableNames Regex
    /// </summary>
    /// <param name="cypher">The Cypher statement.</param>
    /// <returns>The modified string with whitespaces.</returns>
    private static string AddBraceWhitespaces(string cypher)
    {
        var StringContents = ExtractStringContent(cypher);
        var NoStringCypher = RemoveStringContent(cypher);

        NoStringCypher = Regex.Replace(NoStringCypher, @"\(", "( ");
        NoStringCypher = Regex.Replace(NoStringCypher, @"\)", " )");

        return AddStringContents(NoStringCypher, StringContents);
    }

    /// <summary>
    /// Removes single-line (//) and multi-line (/* */) comments from the Cypher query.
    /// </summary>
    /// <param name="cypher">The Cypher query.</param>
    /// <returns>The Cypher query without comments.</returns>
    private static string RemoveComments(string cypher)
    {
        var StringContents = ExtractStringContent(cypher);
        var NoStringCypher = RemoveStringContent(cypher);

        NoStringCypher = Regex.Replace(NoStringCypher, @"//(.*?)$", "");
        NoStringCypher = Regex.Replace(NoStringCypher, @"/\*.*?\*/", "");

        return AddStringContents(NoStringCypher, StringContents);
    }

    /// <summary>
    /// Converts property access to indexer access for Linq (Instance.Properties["..."]).
    /// </summary>
    /// <param name="cypher">The Cypher statement.</param>
    /// <returns>The modified Cypher statement.</returns>
    private static string ReplaceProperties(string cypher)
    {
        var StringContents = ExtractStringContent(cypher);
        var NoStringCypher = RemoveStringContent(cypher);

        var property = nameof(Models.Instance.Properties);

        var result = Regex.Replace(
            NoStringCypher,
            $@"{property}\.(.*?)\s+",
            match => $"{property}[\"{match.Groups[1].Value}\"] "
        );

        return AddStringContents(result, StringContents);
    }

    /// <summary>
    /// Translates logical operators and comparisons from Cypher to C# syntax.
    /// </summary>
    /// <param name="statement">The statement to process.</param>
    /// <returns>The statement with replaced operators.</returns>
    private static string ReplaceOperators(string statement)
    {
        var StringContents = ExtractStringContent(statement);
        var NoStringCypher = RemoveStringContent(statement);

        NoStringCypher = Regex.Replace(NoStringCypher, @"AND", "&&");
        NoStringCypher = Regex.Replace(NoStringCypher, @"OR", "||");
        NoStringCypher = Regex.Replace(NoStringCypher, @"NOT EQUAL\s+", "!= ");
        NoStringCypher = Regex.Replace(NoStringCypher, @"NOT(\s+.*?\s+=\s+.*?($|\s+))", match => $"!({match.Groups[1].Value.Trim()}) ");
        NoStringCypher = Regex.Replace(NoStringCypher, @"(?<![!=<>])=(?!=)", "==");

        return AddStringContents(NoStringCypher, StringContents);
    }

    /// <summary>
    /// Replaces single quotes with double quotes.
    /// </summary>
    /// <param name="statement">The statement to process.</param>
    /// <returns>The statement with quotes replaced.</returns>
    private static string ReplaceSymbols(string statement)
    {
        statement = Regex.Replace(statement, @"'", "\"");

        return statement;
    }

    /// <summary>
    /// Converts text functions (CONTAINS, STARTS WITH, ENDS WITH) to C# string methods.
    /// </summary>
    /// <param name="statement">The statement to process.</param>
    /// <returns>The statement with functions replaced.</returns>
    private static string ReplaceFunctions(string statement)
    {
        var StringContents = ExtractStringContent(statement);
        var NoStringCypher = RemoveStringContent(statement);

        NoStringCypher = Regex.Replace(NoStringCypher, @"\s+CONTAINS\s*("".*?"")(\s|$)", m => $".Contains({m.Groups[1].Value}) ");
        NoStringCypher = Regex.Replace(NoStringCypher, @"\s+STARTS WITH\s*("".*?"")(\s|$)", m => $".StartsWith({m.Groups[1].Value}) ");
        NoStringCypher = Regex.Replace(NoStringCypher, @"\s+ENDS WITH\s*("".*?"")(\s|$)", m => $".EndsWith({m.Groups[1].Value}) ");

        return AddStringContents(NoStringCypher, StringContents); ;
    }

    /// <summary>
    /// Extracts an expression for a given command word (e.g., WHERE, RETURN) in the query.
    /// A cmdWord must be used 1 time
    /// </summary>
    /// <param name="textQuery">The Cypher query as text.</param>
    /// <param name="cmdWord">The command word to search for.</param>
    /// <returns>The extracted expression.</returns>
    private string ExtractCmd(string textQuery, string cmdWord)
    {
        string regex = $@"{cmdWord}\s+(.*?)({string.Join("|", _allowedCmds)}|$)";
        MatchCollection matches = Regex.Matches(textQuery, regex, RegexOptions.Multiline);
        return matches.Cast<Match>()
                .Select(m => m.Groups[1].Value.Trim())
                .ToList().SingleOrDefault() ?? throw new ArgumentException($"{cmdWord} is to be used 1 time.");
    }

    /// <summary>
    /// Extracts variable names for relations from the MATCH clause.
    /// </summary>
    /// <param name="textQuery">The Cypher query as text.</param>
    /// <returns>RelationVariableNames containing Subject, Predicate, Object identifiers.</returns>
    private RelationVariableNames ExtractUserInput(string textQuery)
    {
        var matchLine = ExtractCmd(textQuery, _allowedCmds[(int)Cmds.Match]);
        if (matchLine.IsNullOrEmpty())
            throw new ArgumentException("Match is to be used a single time.");
        var nodes = ExtractNodes(matchLine);
        var direction = ExtractDirection(matchLine);
        var relation = ExtractRelation(matchLine);
        var node1 = ExtractUserIdentifier(nodes[0]);
        var node2 = ExtractUserIdentifier(nodes[1]);
        return new RelationVariableNames
        {
            Subject = direction ? node1 : node2,
            Predicate = ExtractUserIdentifier(relation),
            Object = direction ? node2 : node1
        };
    }

    /// <summary>
    /// Determines the direction of the relation in the MATCH clause.
    /// false: &lt;-- ; true: --&gt;
    /// </summary>
    /// <param name="cypher">Cypher statement containing the relation.</param>
    /// <returns>True if direction is --&gt; otherwise false.</returns>
    private static bool ExtractDirection(string cypher)
    {
        var forwardPattern = @"\)-\[.*?\]->\(";
        var backwardPattern = @"\)<-\[.*?\]-\(";
        if (Regex.IsMatch(cypher, forwardPattern))
            return true;
        if (Regex.IsMatch(cypher, backwardPattern))
            return false;
        throw new ArgumentException("No direction found");
    }

    /// <summary>
    /// Extracts the relation label from the MATCH clause.
    /// </summary>
    /// <param name="cypher">Cypher statement with the MATCH clause.</param>
    /// <returns>The relation label as a string.</returns>
    private static string ExtractRelation(string cypher)
    {
        var relationPattern = @"\[(.*?)\]";
        MatchCollection matches = Regex.Matches(cypher, relationPattern, RegexOptions.Singleline);
        return matches.Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .SingleOrDefault() ?? throw new ArgumentException("No relation found");
    }

    /// <summary>
    /// Extracts the node specifications from the MATCH clause.
    /// </summary>
    /// <param name="cypher">Cypher statement with the MATCH clause.</param>
    /// <returns>List of node specifications.</returns>
    private static List<string> ExtractNodes(string cypher)
    {
        var nodePattern = @"\((.*?)\)";
        MatchCollection matches = Regex.Matches(cypher, nodePattern, RegexOptions.Singleline);
        if (matches.Count != 2) throw new ArgumentException("Count of nodes not equal 2");
        return matches.Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .ToList();
    }

    /// <summary>
    /// Extracts the identifier (before colon) from a node or relation specification.
    /// </summary>
    /// <param name="cypher">Node or relation specification string.</param>
    /// <returns>User-defined identifier.</returns>
    private static string ExtractUserIdentifier(string cypher)
    {
        string[] parts = cypher.Split(':');
        return !parts.IsNullOrEmpty() ? parts[0] : throw new ArgumentException("User Identifier is Empty");
    }

    /// <summary>
    /// Record struct for representing relation variable names in the Cypher query.
    /// </summary>
    public class RelationVariableNames
    {
        /// <summary>
        /// Variable name for the subject node.
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Variable name for the predicate (relation).
        /// </summary>
        public string Predicate { get; set; } = string.Empty;

        /// <summary>
        /// Variable name for the object node.
        /// </summary>
        public string Object { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result type for a Cypher to Linq translation.
    /// </summary>
    public class TranslationResult
    {
        /// <summary>
        /// WHERE clause as Linq expression.
        /// </summary>
        public string LinqWhere { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if the RETURN statement contains the subject variable.
        /// </summary>
        public bool ReturnContainsSubject { get; set; } = false;

        /// <summary>
        /// Indicates if the RETURN statement contains the predicate variable.
        /// </summary>
        public bool ReturnContainsPredicate { get; set; } = false;

        /// <summary>
        /// Indicates if the RETURN statement contains the object variable.
        /// </summary>
        public bool ReturnContainsObject { get; set; } = false;
    }
}