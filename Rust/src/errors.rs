use std::error::Error;
use std::{fmt, io};

use crate::errors::VerifyNodeModulesError::*;

/// Trait that allows getting an error code (numeric) from errors.
pub trait ErrorCode: Error {
    /// Get error code for the error.
    fn error_code(&self) -> i32;
}

#[derive(Debug)]
pub enum VerifyNodeModulesError {
    CouldNotGetCwd(io::Error),
    CouldNotOpenPackageLock(tokio::io::Error),
    CouldNotOpenPackageJson(tokio::io::Error),
    CouldNotParsePackageLock(serde_json::Error),
    CouldNotParsePackageJson(serde_json::Error),
}

impl fmt::Display for VerifyNodeModulesError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match *self {
            CouldNotGetCwd(ref e) => write!(f, "Could not get current working directory: {}", e),
            CouldNotOpenPackageLock(ref e) => write!(f, "Could not open package-lock.json: {}", e),
            CouldNotOpenPackageJson(ref e) => write!(f, "Could not open package.json: {}", e),
            CouldNotParsePackageLock(ref e) => {
                write!(f, "Could not parse package-lock.json: {}", e)
            }
            CouldNotParsePackageJson(ref e) => write!(f, "Could not parse package.json: {}", e),
        }
    }
}

impl Error for VerifyNodeModulesError {
    fn source(&self) -> Option<&(dyn Error + 'static)> {
        match *self {
            CouldNotGetCwd(ref e) => Some(e),
            CouldNotOpenPackageLock(ref e) => Some(e),
            CouldNotOpenPackageJson(ref e) => Some(e),
            CouldNotParsePackageLock(ref e) => Some(e),
            CouldNotParsePackageJson(ref e) => Some(e),
        }
    }
}

impl ErrorCode for VerifyNodeModulesError {
    fn error_code(&self) -> i32 {
        match *self {
            CouldNotGetCwd(_) => -1,
            CouldNotOpenPackageLock(_) => -2,
            CouldNotOpenPackageJson(_) => -3,
            CouldNotParsePackageLock(_) => -4,
            CouldNotParsePackageJson(_) => -5,
        }
    }
}
