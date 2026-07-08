package sentinel

import (
	"context"
	"encoding/json"
	"os"
	"path/filepath"
)

const configFileName = "config.json"

// StoredCredentials represents persisted CLI credentials.
type StoredCredentials struct {
	BaseURL      string `json:"baseUrl"`
	AccessToken  string `json:"accessToken"`
	RefreshToken string `json:"refreshToken"`
	TenantID     string `json:"tenantId"`
	Email        string `json:"email"`
}

// ConfigPath returns the default Sentinel CLI config file path.
func ConfigPath() (string, error) {
	home, err := os.UserHomeDir()
	if err != nil {
		return "", err
	}
	return filepath.Join(home, ".sentinel", configFileName), nil
}

// LoadStoredCredentials reads credentials written by the Sentinel CLI.
func LoadStoredCredentials() (*StoredCredentials, error) {
	path, err := ConfigPath()
	if err != nil {
		return nil, err
	}

	data, err := os.ReadFile(path)
	if err != nil {
		if os.IsNotExist(err) {
			return nil, nil
		}
		return nil, err
	}

	var credentials StoredCredentials
	if err := json.Unmarshal(data, &credentials); err != nil {
		return nil, err
	}

	return &credentials, nil
}

// NewClientFromStoredCredentials builds a client using CLI-stored credentials.
func NewClientFromStoredCredentials() *Client {
	credentials, err := LoadStoredCredentials()
	if err != nil || credentials == nil {
		return NewClient(ClientOptions{})
	}

	return NewClient(ClientOptions{
		BaseURL:      credentials.BaseURL,
		AccessToken:  credentials.AccessToken,
		RefreshToken: credentials.RefreshToken,
		TenantID:     credentials.TenantID,
	})
}

// EnsureValidToken refreshes the access token when only a refresh token is configured.
func (c *Client) EnsureValidToken(ctx context.Context) error {
	if c.options.AccessToken != "" || c.options.RefreshToken == "" {
		return nil
	}

	_, err := c.RefreshToken(ctx)
	return err
}
