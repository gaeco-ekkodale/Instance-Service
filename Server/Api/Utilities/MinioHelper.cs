// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using InstanceService.Models;

namespace InstanceService.Api.Utilities
{
    public interface IMinioHelper
    {
        /// <summary>
        /// Generic method to upload a serialized JSON object to MinIO.
        /// </summary>
        /// <param name="bucketName">Bucket name.</param>
        /// <param name="objectName">Object/file name.</param>
        /// <param name="serializedJson">Serialized json string.</param>
        /// <returns></returns>
        public Task UploadJsonAsync(string bucketName, string objectName, string serializedJson);

        /// <summary>
        /// Get the URL of the object in MinIO.
        /// </summary>
        /// <param name="bucketName">Bucket name.</param>
        /// <param name="objectName">Object/file name.</param>
        /// <returns>Url belonging to the requested file or empty url if not found.</returns>
        public Task<string> GetObjectUrl(string bucketName, string objectName);
    }

    public class MinioHelper : IMinioHelper
    {
        private readonly IMinioClient _minioClient;
        private readonly ILogger<MinioHelper> _logger;

        public MinioHelper(IOptions<MinioOptions> options, ILogger<MinioHelper> logger)
        {
            _logger = logger;

            MinioOptions minioOptions = options.Value;
            _minioClient = new MinioClient()
                .WithEndpoint(minioOptions.Address)
                .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey)
                .WithSSL(minioOptions.Address.StartsWith("https"))
                .Build();
        }

        /// <inheritdoc />
        public async Task UploadJsonAsync(string bucketName, string objectName, string serializedJson)
        {
            bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (!found)
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
            }

            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(serializedJson);

            await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(new MemoryStream(jsonBytes))
                .WithObjectSize(jsonBytes.Length)
                .WithContentType("application/json"));
        }

        /// <inheritdoc />
        public async Task<string> GetObjectUrl(string bucketName, string objectName)
        {
            try
            {
                string url = await _minioClient.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithExpiry(24 * 60 * 60)); // Set expiry time for the link (e.g., 24 hours)

                return url;
            }
            catch (BucketNotFoundException ex)
            {
                _logger.LogError(ex, "Bucket not found for {BucketName} bucket and {Object} object.", bucketName, objectName);
                return string.Empty;
            }
            catch (ObjectNotFoundException ex)
            {
                _logger.LogError(ex, "Object not found for {BucketName} bucket and {Object} object.", bucketName, objectName);
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting the object URL for {BucketName} bucket and {Object} object.", bucketName, objectName);
                throw new InvalidOperationException($"Error occurred while getting the object URL for {bucketName} bucket and {objectName} object", ex);
            }
        }
    }
}
