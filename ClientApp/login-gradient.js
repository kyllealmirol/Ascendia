import React from 'react';
import { createRoot } from 'react-dom/client';
import { ShaderGradient, ShaderGradientCanvas } from '@shadergradient/react';

const mountPoint = document.getElementById('system-gradient-background');

class GradientErrorBoundary extends React.Component {
    state = { hasError: false };

    static getDerivedStateFromError() {
        return { hasError: true };
    }

    componentDidCatch(error) {
        console.error('ShaderGradient failed to render; using the CSS background instead.', error);
    }

    render() {
        return this.state.hasError ? null : this.props.children;
    }
}

if (mountPoint) {
    const testCanvas = document.createElement('canvas');
    const supportsWebGL = Boolean(
        testCanvas.getContext('webgl2') || testCanvas.getContext('webgl')
    );

    if (!supportsWebGL) {
        console.warn('WebGL is unavailable; using the CSS gradient background.');
    } else {
        const gradient = React.createElement(ShaderGradient, {
            animate: 'on',
            brightness: 0.9,
            cAzimuthAngle: 180,
            cDistance: 3.6,
            cPolarAngle: 90,
            cameraZoom: 1,
            color1: '#faff5c',
            color2: '#b981c0',
            color3: '#ffcf75',
            envPreset: 'lobby',
            grain: 'off',
            lightType: '3d',
            positionX: -1.4,
            positionY: 0,
            positionZ: 0,
            range: 'enabled',
            rangeEnd: 40,
            rangeStart: 0,
            reflection: 0.1,
            rotationX: 0,
            rotationY: 10,
            rotationZ: 50,
            shader: 'defaults',
            type: 'plane',
            uAmplitude: 1,
            uDensity: 2.6,
            uFrequency: 5.5,
            uSpeed: 0.5,
            uStrength: 4.5,
            uTime: 0,
            wireframe: false
        });

        createRoot(mountPoint).render(
            React.createElement(
                GradientErrorBoundary,
                null,
                React.createElement(
                    ShaderGradientCanvas,
                    {
                        className: 'system-gradient-canvas',
                        style: { position: 'absolute', inset: 0 },
                        pixelDensity: 2,
                        fov: 45,
                        pointerEvents: 'none',
                        lazyLoad: false,
                        powerPreference: 'low-power'
                    },
                    gradient
                )
            )
        );
    }
}
