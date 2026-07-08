package sentinel

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"net/url"
	"strings"
	"time"
)

// ClientOptions configures the Sentinel API client.
type ClientOptions struct {
	BaseURL          string
	AccessToken      string
	RefreshToken     string
	TenantID         string
	TenantHeaderName string
	Timeout          time.Duration
	MaxRetryAttempts int
	HTTPClient       *http.Client
}

// Client is the official Go SDK for the Sentinel observability platform.
type Client struct {
	options    ClientOptions
	httpClient *http.Client
}

// NewClient creates a Sentinel API client with the provided options.
func NewClient(options ClientOptions) *Client {
	if options.BaseURL == "" {
		options.BaseURL = DefaultBaseURL
	}
	if options.TenantHeaderName == "" {
		options.TenantHeaderName = DefaultTenantHeader
	}
	if options.Timeout == 0 {
		options.Timeout = time.Duration(DefaultTimeout) * time.Second
	}
	if options.MaxRetryAttempts == 0 {
		options.MaxRetryAttempts = DefaultMaxRetryCount
	}

	httpClient := options.HTTPClient
	if httpClient == nil {
		httpClient = &http.Client{Timeout: options.Timeout}
	}

	return &Client{options: options, httpClient: httpClient}
}

// SetAccessToken updates the bearer token and optional tenant ID.
func (c *Client) SetAccessToken(accessToken, tenantID string) {
	c.options.AccessToken = accessToken
	if tenantID != "" {
		c.options.TenantID = tenantID
	}
}

// Login authenticates and stores tokens on the client.
func (c *Client) Login(ctx context.Context, request LoginRequest) (*AuthResponse, error) {
	var response AuthResponse
	if err := c.post(ctx, "/api/v1/auth/login", request, false, &response); err != nil {
		return nil, err
	}

	c.options.AccessToken = response.AccessToken
	c.options.RefreshToken = response.RefreshToken
	c.options.TenantID = response.User.TenantID
	return &response, nil
}

// RefreshToken exchanges the stored refresh token for a new access token.
func (c *Client) RefreshToken(ctx context.Context) (*AuthResponse, error) {
	if c.options.RefreshToken == "" {
		return nil, fmt.Errorf("refresh token is not configured")
	}

	var response AuthResponse
	request := RefreshTokenRequest{RefreshToken: c.options.RefreshToken}
	if err := c.post(ctx, "/api/v1/auth/refresh", request, false, &response); err != nil {
		return nil, err
	}

	c.options.AccessToken = response.AccessToken
	c.options.RefreshToken = response.RefreshToken
	c.options.TenantID = response.User.TenantID
	return &response, nil
}

// IngestLogs sends log entries to Sentinel.
func (c *Client) IngestLogs(ctx context.Context, logs []LogEntryInput) (*IngestLogsResponse, error) {
	var response IngestLogsResponse
	err := c.post(ctx, "/api/v1/logs", IngestLogsRequest{Logs: logs}, true, &response)
	return &response, err
}

// SearchLogs queries ingested logs.
func (c *Client) SearchLogs(ctx context.Context, query LogSearchQuery) (*SearchLogsResponse, error) {
	path := "/api/v1/search/logs" + buildQuery(map[string]string{
		"from":        derefString(query.From),
		"to":          derefString(query.To),
		"level":       derefString(query.Level),
		"service":     derefString(query.Service),
		"environment": derefString(query.Environment),
		"query":       derefString(query.Query),
		"traceId":     derefString(query.TraceID),
		"limit":       intToString(query.Limit),
		"offset":      intToString(query.Offset),
	})

	var response SearchLogsResponse
	err := c.get(ctx, path, &response)
	return &response, err
}

// IngestMetrics sends metric samples to Sentinel.
func (c *Client) IngestMetrics(ctx context.Context, metrics []MetricEntryInput) (*IngestMetricsResponse, error) {
	var response IngestMetricsResponse
	err := c.post(ctx, "/api/v1/metrics", IngestMetricsRequest{Metrics: metrics}, true, &response)
	return &response, err
}

// QueryMetrics retrieves stored metric samples.
func (c *Client) QueryMetrics(ctx context.Context, query MetricQuery) (*QueryMetricsResponse, error) {
	path := "/api/v1/metrics" + buildQuery(map[string]string{
		"from":        derefString(query.From),
		"to":          derefString(query.To),
		"name":        derefString(query.Name),
		"service":     derefString(query.Service),
		"environment": derefString(query.Environment),
		"limit":       intToString(query.Limit),
	})

	var response QueryMetricsResponse
	err := c.get(ctx, path, &response)
	return &response, err
}

// ListAlerts returns alert rules for the active tenant.
func (c *Client) ListAlerts(ctx context.Context) ([]AlertRule, error) {
	var response []AlertRule
	err := c.get(ctx, "/api/v1/alerts", &response)
	return response, err
}

// GetAlert returns a single alert rule by ID.
func (c *Client) GetAlert(ctx context.Context, id string) (*AlertRule, error) {
	var response AlertRule
	err := c.get(ctx, "/api/v1/alerts/"+url.PathEscape(id), &response)
	return &response, err
}

// ListIncidents returns incidents for the active tenant.
func (c *Client) ListIncidents(ctx context.Context) ([]Incident, error) {
	var response []Incident
	err := c.get(ctx, "/api/v1/incidents", &response)
	return response, err
}

// GetIncident returns a single incident by ID.
func (c *Client) GetIncident(ctx context.Context, id string) (*Incident, error) {
	var response Incident
	err := c.get(ctx, "/api/v1/incidents/"+url.PathEscape(id), &response)
	return &response, err
}

// ListTenants returns tenants accessible to the authenticated user.
func (c *Client) ListTenants(ctx context.Context) ([]Tenant, error) {
	var response []Tenant
	err := c.get(ctx, "/api/v1/tenants", &response)
	return response, err
}

// GetTenant returns a tenant by ID.
func (c *Client) GetTenant(ctx context.Context, id string) (*Tenant, error) {
	var response Tenant
	err := c.get(ctx, "/api/v1/tenants/"+url.PathEscape(id), &response)
	return &response, err
}

func (c *Client) get(ctx context.Context, path string, out any) error {
	return c.request(ctx, http.MethodGet, path, nil, true, out)
}

func (c *Client) post(ctx context.Context, path string, body any, authenticated bool, out any) error {
	return c.request(ctx, http.MethodPost, path, body, authenticated, out)
}

func (c *Client) request(ctx context.Context, method, path string, body any, authenticated bool, out any) error {
	var payload []byte
	var err error
	if body != nil {
		payload, err = json.Marshal(body)
		if err != nil {
			return err
		}
	}

	endpoint := strings.TrimRight(c.options.BaseURL, "/") + path
	var lastErr error

	for attempt := 0; attempt <= c.options.MaxRetryAttempts; attempt++ {
		var reader io.Reader
		if payload != nil {
			reader = bytes.NewReader(payload)
		}

		req, err := http.NewRequestWithContext(ctx, method, endpoint, reader)
		if err != nil {
			return err
		}

		req.Header.Set("Accept", "application/json")
		if payload != nil {
			req.Header.Set("Content-Type", "application/json")
		}

		if authenticated && c.options.AccessToken != "" {
			req.Header.Set("Authorization", "Bearer "+c.options.AccessToken)
		}
		if authenticated && c.options.TenantID != "" {
			req.Header.Set(c.options.TenantHeaderName, c.options.TenantID)
		}

		resp, err := c.httpClient.Do(req)
		if err != nil {
			lastErr = err
			time.Sleep(time.Duration(1<<attempt) * 100 * time.Millisecond)
			continue
		}

		responseBody, err := io.ReadAll(resp.Body)
		resp.Body.Close()
		if err != nil {
			return err
		}

		if resp.StatusCode >= 200 && resp.StatusCode < 300 {
			if out == nil || len(responseBody) == 0 {
				return nil
			}
			return json.Unmarshal(responseBody, out)
		}

		message := parseErrorMessage(responseBody)
		if message == "" {
			message = resp.Status
		}

		apiErr := &APIError{StatusCode: resp.StatusCode, Message: message, ResponseBody: string(responseBody)}
		if resp.StatusCode == http.StatusTooManyRequests || resp.StatusCode >= 500 {
			lastErr = apiErr
			time.Sleep(time.Duration(1<<attempt) * 100 * time.Millisecond)
			continue
		}

		return apiErr
	}

	if lastErr != nil {
		return lastErr
	}

	return fmt.Errorf("request failed")
}

func buildQuery(params map[string]string) string {
	values := url.Values{}
	for key, value := range params {
		if value != "" {
			values.Set(key, value)
		}
	}

	encoded := values.Encode()
	if encoded == "" {
		return ""
	}

	return "?" + encoded
}

func derefString(value *string) string {
	if value == nil {
		return ""
	}
	return *value
}

func intToString(value int) string {
	if value == 0 {
		return ""
	}
	return fmt.Sprintf("%d", value)
}

func parseErrorMessage(body []byte) string {
	var payload map[string]any
	if err := json.Unmarshal(body, &payload); err != nil {
		return ""
	}

	if value, ok := payload["error"].(string); ok {
		return value
	}
	if value, ok := payload["title"].(string); ok {
		return value
	}

	return ""
}
