// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import React, { useEffect, useState } from "react";
import { OpenAPI } from "@/api/orchestrator/core/OpenAPI";
import { CircularProgress } from "@mui/material";

interface ProtectedImageProps {
  url: string;
  alt?: string;
  className?: string;
  style?: React.CSSProperties;
  width?: number | string;
  height?: number | string;
  showLoader?: boolean;
}

/**
 * ProtectedImage - Component for loading images through authenticated API
 *
 * @param url - The URL to fetch the image from
 * @param alt - Alternative text for the image
 * @param className - CSS class for styling
 * @param style - Inline styles
 * @param width - Width of the image (number for pixels, string for other units)
 * @param height - Height of the image (number for pixels, string for other units)
 * @param showLoader - Whether to show a loading spinner (default: true)
 */
const ProtectedImage: React.FC<ProtectedImageProps> = ({
  url,
  alt = "",
  className = "",
  style,
  width,
  height,
  showLoader = true,
}) => {
  const [imageSrc, setImageSrc] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => {
    let objectUrl: string | null = null;
    let isMounted = true;

    const loadImage = async () => {
      try {
        setLoading(true);
        setError(false);

        const token =
          typeof OpenAPI.TOKEN === "function"
            ? await OpenAPI.TOKEN({} as any)
            : OpenAPI.TOKEN;

        const absoluteUrl = url.startsWith("/") ? `${OpenAPI.BASE}${url}` : url;
        const response = await fetch(absoluteUrl, {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        });

        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }

        if (!isMounted) return;

        const blob = await response.blob();
        objectUrl = URL.createObjectURL(blob);
        setImageSrc(objectUrl);
      } catch (err) {
        console.error("Error loading protected image:", err);
        if (isMounted) {
          setError(true);
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    loadImage();

    return () => {
      isMounted = false;
      if (objectUrl) {
        URL.revokeObjectURL(objectUrl);
      }
    };
  }, [url]);

  const combinedStyle: React.CSSProperties = {
    ...style,
    ...(width !== undefined && {
      width: typeof width === "number" ? `${width}px` : width,
    }),
    ...(height !== undefined && {
      height: typeof height === "number" ? `${height}px` : height,
    }),
  };

  if (loading && showLoader) {
    return (
      <div
        className={`flex items-center justify-center ${className}`}
        style={combinedStyle}
      >
        <CircularProgress size={24} />
      </div>
    );
  }

  if (error) {
    return (
      <div
        className={`flex items-center justify-center bg-gray-200 ${className}`}
        style={combinedStyle}
      >
        <span className="text-gray-500 text-sm">Image not available</span>
      </div>
    );
  }

  return (
    <img
      src={imageSrc || ""}
      alt={alt}
      className={className}
      style={combinedStyle}
    />
  );
};

export default ProtectedImage;
